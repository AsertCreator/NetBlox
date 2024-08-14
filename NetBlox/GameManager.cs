global using Color = Raylib_cs.Color;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.GUIs;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);

	/// <summary>
	/// Represents a NetBlox game. Believe it or not, but one NetBlox process can run multiple games at once (in theory)
	/// </summary>
	public class GameManager
	{
		public List<Instance> AllInstances = [];
		public Dictionary<KeyboardKey, Action> Verbs = [];
		public NetworkIdentity CurrentIdentity = new();
		public RenderManager RenderManager;
		public PhysicsManager PhysicsManager;
		public NetworkManager NetworkManager;
		public DataModel CurrentRoot = null!;
		public ProfileManager CurrentProfile = new();
		public ConfigFlags CustomFlags;
		public bool IsStudio = false;
		public bool IsRunning = true;
		public bool ShuttingDown = false;
		public bool ProhibitProcessing = false;
		public bool ProhibitScripts = false;
		public bool MainManager = false;
		public bool UsePublicService = false;
		public bool FilteringEnabled = true;
		public string QueuedTeleportAddress = "";
		public string ManagerName = "";
		public int PropertyReplicationRate = 20;
		public Dictionary<RemoteClient, Instance> Owners = [];
		public List<Instance> SelfOwnerships = [];
		public ClientStartupInfo? ClientStartupInfo;
		public ServerStartupInfo? ServerStartupInfo;
		public Dictionary<ModuleScript, DynValue> LoadedModules = new();
		public MoonSharp.Interpreter.Script MainEnvironment = null!;
		public string Username => CurrentProfile.Username; // bye bye DevDevDev
		public event EventHandler? ShutdownEvent;
		public bool AllowReplication = false;

		public GameManager(GameConfiguration gc, string[] args, Action<GameManager> loadcallback, Action<DataModel>? dmc = null)
		{
			try
			{
				LogManager.LogInfo("Initializing NetBlox...");
				ManagerName = gc.GameName;

				string? csdata = args[args.ToList().IndexOf("-cs") + 1].Replace("^^", "\"");
				string? ssdata = args[args.ToList().IndexOf("-ss") + 1].Replace("^^", "\"");

				if (gc.AsClient)
					ClientStartupInfo = csdata != null ? SerializationManager.DeserializeJson<ClientStartupInfo>(csdata) : null;
				if (gc.AsServer)
					ServerStartupInfo = ssdata != null ? SerializationManager.DeserializeJson<ServerStartupInfo>(ssdata) : null;

				if (ClientStartupInfo == null && gc.AsClient)
					throw new Exception("Missing startup info");
				if (ServerStartupInfo == null && gc.AsServer)
					throw new Exception("Missing startup info");

				AppManager.PublicServiceAPI = 
					gc.AsServer ? 
					(ServerStartupInfo ?? throw new Exception()).PublicServiceAPI :
					(ClientStartupInfo ?? throw new Exception()).PublicServiceAPI;

				NetworkManager = new(this, gc.AsServer, gc.AsClient);
				CurrentIdentity.Reset();
				IsStudio = gc.AsStudio;

				if (gc.AsClient && (ClientStartupInfo ?? throw new Exception()).IsGuest)
					CurrentProfile.LoginAsGuest();

				if (gc.AsClient)
					LogManager.LogInfo("Logged in as " + Username);

				ProhibitProcessing = gc.ProhibitProcessing;
				ProhibitScripts = gc.ProhibitScripts;

				LogManager.LogInfo("Initializing PhysicsManager...");
				PhysicsManager = new(this);

				CustomFlags = gc.CustomFlags;
				LogManager.LogInfo("Initializing RenderManager...");
				RenderManager = new(this, gc.SkipWindowCreation, !gc.DoNotRenderAtAll, gc.VersionMargin);

				LogManager.LogInfo("Initializing verbs...");
				Verbs.Add(KeyboardKey.Comma, () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);
				Verbs.Add(KeyboardKey.Apostrophe, () => RenderManager.DebugInformation = !RenderManager.DebugInformation);
				Verbs.Add(KeyboardKey.P, () => 
				{ 
					PhysicsManager.DisablePhysics = !PhysicsManager.DisablePhysics;
					if (!PhysicsManager.DisablePhysics)
						PhysicsManager.Begin();
				});

				// we dont want corescripts to run before engine is initialized

				LogManager.LogInfo("Initializing internal scripts...");

				CurrentRoot = new DataModel(this);
				if (dmc != null)
					dmc(CurrentRoot);

				LuaRuntime.Setup(this);
				LogManager.LogInfo("Initializing user interface...");
				SetupCoreGui();

				if (NetworkManager.IsClient)
				{
					LogManager.LogInfo("Creating main services...");
					CurrentRoot.GetService<SandboxService>();
					CurrentRoot.GetService<Debris>();
				}
				if (NetworkManager.IsServer)
				{
					LogManager.LogInfo("Creating main services...");
					CurrentRoot.GetService<Workspace>();
					CurrentRoot.GetService<Players>();
					CurrentRoot.GetService<Lighting>();
					CurrentRoot.GetService<ReplicatedStorage>();
					CurrentRoot.GetService<ReplicatedFirst>();
					CurrentRoot.GetService<StarterGui>();
					CurrentRoot.GetService<StarterPack>();
					CurrentRoot.GetService<ServerStorage>();
					CurrentRoot.GetService<ScriptContext>();
					CurrentRoot.GetService<PlatformService>();
					CurrentRoot.GetService<UserInputService>();
					CurrentRoot.GetService<Chat>();
				}

				var rs = CurrentRoot.GetService<RunService>();
				var cg = CurrentRoot.GetService<CoreGui>();
				rs.Parent = CurrentRoot;
				cg.Parent = CurrentRoot;

				if (NetworkManager.IsClient)
				{
					CurrentRoot.GetService<CoreGui>().ShowTeleportGui("", "", -1, -1);
					QueuedTeleportAddress = (ClientStartupInfo ?? throw new Exception()).ServerIP;
				}

				loadcallback(this);
			}
			catch (Exception ex)
			{
				LogManager.LogError("A fatal error had occurred during NetBlox initialization! " + ex.GetType() + ", msg: " + ex.Message + ", stacktrace: " + ex.StackTrace);
				Environment.Exit(ex.GetHashCode());
				for (;;); // perhaps platform we're running on does not support exiting.
			}
		}
		public void SetupCoreGui()
		{
			CoreGui cg = CurrentRoot.GetService<CoreGui>();
			ScreenGui sg = new(this);
			sg.Name = "RobloxGui"; // i love breaking copyright :D
			sg.Parent = cg;

			// apparently roblox does not just load all corescritps on bulk.
			var scrurl = AppManager.ResolveUrlAsync("rbxasset://scripts/Modules/", false).WaitAndGetResult();
			string? ssurl;
			if (NetworkManager.IsServer)
				ssurl = AppManager.ResolveUrlAsync("rbxasset://scripts/ServerStarterScript.lua", false).WaitAndGetResult();
			else
				ssurl = AppManager.ResolveUrlAsync("rbxasset://scripts/StarterScript.lua", false).WaitAndGetResult();

			if (!File.Exists(ssurl))
				throw new Exception("No StarterScript found in content directory!");

			var Modules = new Folder(this);
			Modules.Name = "Modules";
			Modules.Parent = sg;
			var files = Directory.GetFiles(scrurl);

			for (int i = 0; i < files.Length; i++)
			{
				ModuleScript ms = new(this);
				ms.Name = Path.GetFileNameWithoutExtension(files[i]);
				ms.Source = File.ReadAllText(files[i]);
				ms.Parent = Modules;
			}

			CoreScript ss = new(this);
			ss.Name = "StarterScript";
			ss.Source = File.ReadAllText(ssurl);
			ss.Parent = sg;
		}
		public void Shutdown()
		{
			LogManager.LogInfo($"Shutting down GameManager \"{ManagerName}\"...");
			ShuttingDown = true;
			ShutdownEvent?.Invoke(new(), new());
			AppManager.GameManagers.Remove(this);

			if (AppManager.CurrentRenderManager == RenderManager)
				AppManager.CurrentRenderManager = null;

			if (RenderManager != null)
				RenderManager.Unload();
			RenderManager = null;

			if (MainManager)
				Environment.Exit(0);
		}
		public void LoadDefault(int idx = 0)
		{
			LogManager.LogInfo("Loading default place...");
			Workspace ws = CurrentRoot.GetService<Workspace>();
			ReplicatedStorage rs = CurrentRoot.GetService<ReplicatedStorage>();
			ReplicatedFirst ri = CurrentRoot.GetService<ReplicatedFirst>();
			Players pl = CurrentRoot.GetService<Players>();

			switch (idx)
			{
				case 1:
					{
						Part part = new(this)
						{
							Parent = ws,
							Color3 = Color.DarkGreen,
							Position = new(0, -45f, 0),
							Size = new(512, 2, 512),
							TopSurface = SurfaceType.Studs,
							Anchored = true
						};

						for (int i = 0; i < 7; i++)
						{
							for (int j = 0; j < i; j++)
							{
								_ = new Part(this)
								{
									Parent = ws,
									Color3 = Color.White,
									Position = new(i * 1.5f, 60 + j * 1.5f, i * 1.5f),
									Size = new(1, 1, 1),
									TopSurface = SurfaceType.Studs,
									BottomSurface = SurfaceType.Studs,
									LeftSurface = SurfaceType.Studs,
									RightSurface = SurfaceType.Studs,
									FrontSurface = SurfaceType.Studs,
									BackSurface = SurfaceType.Studs,
								};
							}
						}

						_ = new Part(this)
						{
							Parent = ws,
							Color3 = Color.White,
							Position = new(-10, 70, -10),
							Size = new(1, 40, 1),
							TopSurface = SurfaceType.Studs,
							BottomSurface = SurfaceType.Studs,
							LeftSurface = SurfaceType.Studs,
							RightSurface = SurfaceType.Studs,
							FrontSurface = SurfaceType.Studs,
							BackSurface = SurfaceType.Studs,
						};

						break;
					}
				default:
					{
						LocalScript ls = new(this);

						ws.ZoomToExtents();
						ws.Parent = CurrentRoot;

						Part part = new(this)
						{
							Parent = ws,
							Color3 = Color.DarkGreen,
							Position = new(0, -45f, 0),
							Size = new(32, 2, 32),
							TopSurface = SurfaceType.Studs,
							Anchored = true
						};
						new SpawnLocation(this)
						{
							Parent = ws,
							Position = new(0, -45f + 2, 0),
							TopSurface = SurfaceType.Studs
						};

						new Part(this)
						{
							Parent = ws,
							Color3 = Color.DarkBlue,
							Position = new(0, -3f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						new Part(this)
						{
							Parent = ws,
							Color3 = Color.DarkBlue,
							Position = new(-1, -3f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						new Part(this)
						{
							Parent = ws,
							Color3 = Color.Red,
							Position = new(-0.5f, -1f, 0),
							Size = new(2, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						new Part(this)
						{
							Parent = ws,
							Color3 = Color.Yellow,
							Position = new(-2f, -1f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						new Part(this)
						{
							Parent = ws,
							Color3 = Color.Yellow,
							Position = new(1f, -1f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};

						ls.Parent = ri;
						ls.Source = "print(\"HIIIIII\"); printidentity();";
						break;
					}
			}

			CurrentIdentity.MaxPlayerCount = 8;
			CurrentIdentity.PlaceName = "Default Place";
			CurrentIdentity.UniverseName = "NetBlox Defaults";
			CurrentIdentity.Author = "The Lord";
			CurrentIdentity.PlaceID = 47384;
			CurrentIdentity.UniverseID = 47384;

			CurrentRoot.Name = CurrentIdentity.PlaceName;
		}
		public Instance? GetInstance(Guid id)
		{
			for (int i = 0; i < AllInstances.Count; i++)
			{
				if (AllInstances[i].UniqueID == id)
					return AllInstances[i];
			}
			return null;
		}
		public void ProcessInstance(Instance inst)
		{
			try
			{
				if (inst != null)
				{ // i was outsmarted
					if (inst.DestroyAt < DateTime.UtcNow)
					{
						inst.Destroy();
						return;
					}

					inst.Process();

					var ch = inst.GetChildren();
					for (int i = 0; i < ch.Length; i++)
					{
						ProcessInstance(ch[i]);
					}
				}
			}
			catch
			{
				// no
			}
		}
		public string FilterString(string text)
		{
			if (!UsePublicService)
			{
				return "lol";
			}
			return "c'est ma chatte";
		}
	}
}
