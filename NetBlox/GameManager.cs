global using Color = Raylib_cs.Color;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;
using System.Runtime;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);
	public static class GameManager
	{
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastNumbers = [];
		public static Dictionary<char, Action> Verbs = [];
		public static List<CoreScript> CoreScripts = new();
		public static List<Instance> AllInstances = [];
		public static List<NetworkClient> AllClients = [];
		public static List<Instance> CrossDataModelInstances = [];
		public static NetworkIdentity CurrentIdentity = new();
		public static DataModel CurrentRoot = null!;
		public static DataModel SpecialRoot = null!;
		public static bool IsRunning = false;
		public static bool ShuttingDown = false;
		public static string ContentFolder = "content/";
		public static string? Username = "DevDevDev" + Random.Shared.Next(1000, 9999);
		public static event EventHandler? ShutdownEvent;
		public static event InstanceEventHandler? AddedInstance;
		public static bool AllowReplication = false;
		public const int VersionMajor = 2;
		public const int VersionMinor = 0;
		public const int VersionPatch = 3;

		public static void InvokeAddedEvent(Instance inst)
		{
			if (AddedInstance != null && AllowReplication)
				AddedInstance(inst);
		}
		public static void LoadAllCoreScripts()
		{
			string[] files = Directory.GetFiles(ContentFolder + "scripts");
			for (int i = 0; i < files.Length; i++)
			{
				CoreScript cs = new();
				string cont = File.ReadAllText(files[i]);
				cs.Source = cont;
				cs.IsServerOnly = cont.Contains("-- [serveronly]\n");
				cs.IsClientOnly = cont.Contains("-- [clientonly]\n");
				cs.IsAsync = cont.Contains("-- [async]\n");
				CoreScripts.Add(cs);
			}
		}
		public static void Start(bool client, bool server, bool render, string[] args, Action<string> servercallback)
		{
			ulong pid = ulong.MaxValue;
			string rbxlinit = "";
			LogManager.LogInfo("Initializing NetBlox...");

			NetworkManager.Initialize(server, client);
			CurrentIdentity.Reset();

			// common thingies
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--fast-flag":
						{
							var key = args[++i];
							var bo = int.Parse(args[++i]) == 1;

							FastFlags[key] = bo;
							LogManager.LogInfo($"Setting fast flag {key} to {bo}");
							break;
						}
					case "--fast-string":
						{
							var key = args[++i];
							var st = args[++i];

							FastStrings[key] = st;
							LogManager.LogInfo($"Setting fast stirng {key} to \"{st}\"");
							break;
						}
					case "--fast-number":
						{
							var key = args[++i];
							var nu = int.Parse(args[++i]);

							FastNumbers[key] = nu;
							LogManager.LogInfo($"Setting fast number {key} to {nu}");
							break;
						}
					case "--placeor":
						{
							CurrentIdentity.PlaceName = args[++i];
							break;
						}
					case "--univor":
						{
							CurrentIdentity.UniverseName = args[++i];
							break;
						}
					case "--maxplayers":
						{
							CurrentIdentity.MaxPlayerCount = uint.Parse(args[++i]);
							break;
						}
					case "--rbxl":
						{
							rbxlinit = args[++i];
							break;
						}
					default:
						{
							LogManager.LogError($"Unknown console argument: {args[i]}");
							break;
						}
				}
			}

			LogManager.LogInfo("Initializing verbs...");
			Verbs.Add(',', () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);

			LogManager.LogInfo("Initializing RenderManager...");
			RenderManager.Initialize(render);

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

			LogManager.LogInfo("Initializing internal scripts...");
			SpecialRoot = new();
			SpecialRoot.Name = "csdm";

			var rs = new RunService();
			rs.Parent = SpecialRoot;
			CrossDataModelInstances.Add(rs);

            LuaRuntime.Setup(SpecialRoot, true);
			ExecuteCoreScripts();

			CoreScripts.Clear();

			if (NetworkManager.IsClient)
			{
				RenderManager.DebugViewInfo.ShowSC = true;
				GetSpecialService<CoreGui>().ShowTeleportGui("", "", -1, -1);
			}
			if (NetworkManager.IsServer)
			{
				AddedInstance += (x) =>
				{
					NetworkManager.ToReplicate.Enqueue(new()
					{
						What = x
					});
				};
				servercallback(rbxlinit);
			}

			while (!ShuttingDown) ;
		}
		public static void ExecuteCoreScripts()
		{
			LoadAllCoreScripts();

			for (int i = 0; i < CoreScripts.Count; i++)
			{
				var cs = CoreScripts[i];
				var wait = true;

				LuaRuntime.Execute(cs.Source, 3, null, SpecialRoot, () => wait = false);

				while (wait) ;
			}
		}
		public static void SetupCrossDataModelInstances(DataModel target)
		{
			for (int i = 0; i < CrossDataModelInstances.Count; i++)
			{
				var ins = CrossDataModelInstances[i];
				ins.Parent = target;
				ins.Tables[target.MainEnv] = LuaRuntime.MakeInstanceTable(ins, target.MainEnv);
			}
		}
		public static void TeleportToPlace(ulong pid)
		{
			if (NetworkManager.IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			LogManager.LogInfo($"Teleporting to the place ({pid})...");
			// no actual servers as of now, so just hardcoded values
			string pname = "Testing Place";
			ulong pauth = 1;
			LogManager.LogInfo($"Place has name ({pname}) and author ({pauth})...");
			NetworkManager.ConnectToServer(null!);
		}
		public static void Shutdown()
		{
			LogManager.LogInfo("Shutting down...");
			ShuttingDown = true;
			ShutdownEvent?.Invoke(new(), new());
		}
		public static void Schedule()
		{
			if (LuaRuntime.CurrentThread != null)
			{
				if (LuaRuntime.CurrentThread.Value.Coroutine == null)
					LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
				else if (DateTime.Now >= LuaRuntime.CurrentThread.Value.WaitUntil &&
					LuaRuntime.CurrentThread.Value.Coroutine.State != CoroutineState.Dead)
				{
					var cst = new CancellationTokenSource();
#if !DISABLE_EME
#pragma warning disable SYSLIB0046 // Type or member is obsolete
					var tsk = Task.Run(() =>
					{
						ControlledExecution.Run(() =>
						{
#endif
							try
							{
								if (LuaRuntime.CurrentThread == null)
								{
									if (LuaRuntime.Threads.Count > 0)
										LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
									else
										return;
								}

								var res = LuaRuntime.CurrentThread.Value.Coroutine.Resume();
								if (LuaRuntime.CurrentThread.Value.Coroutine.State != MoonSharp.Interpreter.CoroutineState.Dead ||
									res == null)
								{
									return;
								}
								else
								{
									if (LuaRuntime.Threads.Contains(LuaRuntime.CurrentThread.Value))
									{
										var ac = LuaRuntime.CurrentThread.Value.FinishCallback;
										if (ac != null) ac();
										LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
									}
								}
							}
							catch (ScriptRuntimeException ex)
							{
								LogManager.LogError(ex.Message);
								for (int i = 0; i < ex.CallStack.Count; i++)
									LogManager.LogError($"    at {ex.CallStack[i]}");

								if (LuaRuntime.Threads.Contains(LuaRuntime.CurrentThread.Value))
									LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
							}
#if !DISABLE_EME
#pragma warning restore SYSLIB0046 // Type or member is obsolete
						}, cst.Token);
					});
					if (!tsk.Wait(LuaRuntime.ScriptExecutionTimeout * 1000))
					{
						LuaRuntime.PrintError("Exhausted maximum script execution time!");
						var ac = LuaRuntime.CurrentThread.Value.FinishCallback;
						if (ac != null) ac();
						cst.Cancel();
					}
#endif
				}

				if (LuaRuntime.Threads.Count > 0)
					LuaRuntime.CurrentThread = LuaRuntime.CurrentThread.Next;
				else
					LuaRuntime.CurrentThread = null;
			}
			else
			{
				LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
			}
		}
		public static Instance? GetInstance(Guid id)
		{
			for (int i = 0; i < AllInstances.Count; i++)
			{
				if (AllInstances[i].UniqueID == id)
					return AllInstances[i];
			}
			return null;
		}
		public static void ProcessInstance(Instance inst)
		{
			if (inst != null)
			{ // i was outsmarted
				inst.Process();

				var ch = inst.GetChildren();
				for (int i = 0; i < ch.Length; i++)
				{
					ProcessInstance(ch[i]);
				}
			}
		}
		public static T GetSpecialService<T>() where T : Instance
		{
			for (int i = 0; i < CrossDataModelInstances.Count; i++)
				if (CrossDataModelInstances[i] is T)
					return (T)CrossDataModelInstances[i];
			return null!;
		}
	}
	public class CoreScript
	{
		public string Source = "";
		public bool IsAsync;
		public bool IsServerOnly;
        public bool IsClientOnly;
    }
}