global using Color = Raylib_cs.Color;
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
		public static void Start(bool client, bool server, string[] args)
		{
			ulong pid = ulong.MaxValue;
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
					case "--place-id":
						{
							pid = ulong.Parse(args[++i]);
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
			RenderManager.Initialize();

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

            LogManager.LogInfo("Initializing internal scripts...");
			SpecialRoot = new();
			SpecialRoot.Name = "csdm";
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
				LoadServer();
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

                LuaRuntime.Execute(cs.Source, 8, null, SpecialRoot, () => wait = false);

                while (wait) ;
            }
        }
		public static void SetupCrossDataModelInstances(DataModel target)
        {
			for (int i = 0; i < CrossDataModelInstances.Count; i++)
			{
				var ins = CrossDataModelInstances[i];
				ins.Parent = target;
				ins.LuaTable = LuaRuntime.MakeInstanceTable(ins, target.MainEnv, true);
			}
        }
		public static void LoadServer()
		{
			DataModel dm = new();
			Workspace ws = new();
			ReplicatedStorage rs = new();
			ReplicatedFirst ri = new();
			RunService ru = new();
			Players pl = new();
			LocalScript ls = new();

			ws.ZoomToExtents();
			ws.Parent = dm;
			dm.Name = "Baseplate";

			Part part = new()
			{
				Parent = ws,
				Color = Color.DarkGreen,
				Position = new(0, -5, 0),
				Size = new(50, 2, 20),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};

			ls.Parent = ri;
			ls.Source = "print(\"HIIIIII\")";

			rs.Parent = dm;
			ri.Parent = dm;
			pl.Parent = dm;
			ru.Parent = dm;

			CurrentIdentity.MaxPlayerCount = 8;
			CurrentIdentity.PlaceName = "Default Place";
			CurrentIdentity.UniverseName = "NetBlox Defaults";
			CurrentIdentity.Author = "The Lord";
			CurrentIdentity.PlaceID = 47384;
			CurrentIdentity.UniverseID = 47384;

			CurrentRoot?.Destroy();
			CurrentRoot = dm;

			LuaRuntime.Setup(CurrentRoot, false);
			LuaRuntime.Execute(string.Empty, 0, null, CurrentRoot); // we will run nothing to initialize lua

			NetworkManager.StartServer();

			IsRunning = true;
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
					LuaRuntime.CurrentThread.Value.Coroutine.State != MoonSharp.Interpreter.CoroutineState.Dead)
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
							catch (Exception ex)
							{
								LogManager.LogError(ex.Message);
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
			inst.Process();
			var ch = inst.GetChildren();
			for (int i = 0; i < ch.Length; i++)
			{
				ProcessInstance(ch[i]);
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