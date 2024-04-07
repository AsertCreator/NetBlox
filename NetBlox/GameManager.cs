#define DISABLE_EME

global using Color = Raylib_cs.Color;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);
	public static class GameManager
	{
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastNumbers = [];
		public static Dictionary<char, Action> Verbs = [];
		public static List<Instance> AllInstances = [];
		public static List<NetworkClient> AllClients = [];
		public static NetworkIdentity CurrentIdentity = new();
		public static DataModel CurrentRoot = null!;
		public static bool IsRunning = false;
		public static bool ShuttingDown = false;
		public static string ContentFolder = "content/";
		public static string? Username = "DevDevDev" + Random.Shared.Next(1000, 9999);
		public static event EventHandler? ShutdownEvent;
		public static event InstanceEventHandler AddedInstance;
		public const int VersionMajor = 2;
		public const int VersionMinor = 0;
		public const int VersionPatch = 1;

		public static void InvokeAddedEvent(Instance inst)
		{
			if (AddedInstance != null)
				AddedInstance(inst);
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

			if (NetworkManager.IsClient)
			{
				RenderManager.ShowTeleportGui();
				RenderManager.DebugViewInfo.ShowSC = true;
				AddedInstance += (x) =>
				{
					NetworkManager.ToReplicate.Enqueue(new()
					{
						What = x
					});
				};
			}
			if (NetworkManager.IsServer)
			{
				LoadServer();
			}

			while (!ShuttingDown) ;
		}
		public static void LoadServer()
		{
			DataModel dm = new();
			Workspace ws = new();
			ReplicatedStorage rs = new();
			ReplicatedFirst ri = new();
			RunService ru = new();
			Players pl = new();

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

			LuaRuntime.Setup(CurrentRoot);
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
										LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
								}
							}
							catch
							{
								LogManager.LogError("Scheduler fault! Deleting faulty thread...");
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
		public static T? GetService<T>() where T : Instance
		{
			foreach (var inst in CurrentRoot.Children)
				if (inst is T t)
					return t;
			return null;
		}
	}
}