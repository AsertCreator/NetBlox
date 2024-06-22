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

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);

	/// <summary>
	/// Represents a NetBlox game. Believe it or not, but one NetBlox process can run multiple games at once (in theory)
	/// </summary>
	public class GameManager
	{
		public List<Instance> AllInstances = [];
		public Dictionary<char, Action> Verbs = [];
		public NetworkIdentity CurrentIdentity = new();
		public RenderManager? RenderManager;
		public PhysicsManager? PhysicsManager;
		public NetworkManager? NetworkManager;
		public DataModel CurrentRoot = null!;
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
		public string Username => Profile.Username; // bye bye DevDevDev
		public event EventHandler? ShutdownEvent;
		public event InstanceEventHandler? AddedInstance;
		public bool AllowReplication = false;

		public void InvokeAddedEvent(Instance inst)
		{
			if (AddedInstance != null && AllowReplication)
				AddedInstance(inst);
		}
		public void SetupCoreGui()
		{
			CoreGui cg = CurrentRoot.GetService<CoreGui>();
			ScreenGui sg = new(this);
			sg.Name = "RobloxGui"; // i love breaking copyright :D
			sg.Parent = cg;

			// apparently roblox does not just load all corescritps on bulk.
			var scrurl = AppManager.ResolveUrl("rbxasset://scripts/Modules/");
			var ssurl = AppManager.ResolveUrl("rbxasset://scripts/StarterScript.lua");
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
		public void Start(GameConfiguration gc, string[] args, Action<string, GameManager> servercallback)
		{
			try
			{
				ulong pid = ulong.MaxValue;
				string rbxlinit = "";
				LogManager.LogInfo("Initializing NetBlox...");

				NetworkManager = new(this, gc.AsServer, gc.AsClient);
				CurrentIdentity.Reset();
				IsStudio = gc.AsStudio;

				// common thingies
				for (int i = 0; i < args.Length; i++)
				{
					switch (args[i])
					{
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
						case "--guest":
							{
								Profile.LoginAsGuest();
								break;
							}
						case "--login":
							{
								var user = args[++i];
								var pass = args[++i];
								var task = Profile.LoginAsync(user, pass);
								task.Wait();
								if (task.Result == null)
								{
									LogManager.LogWarn("Login failed, logging as guest...");
									Profile.LoginAsGuest();
								}
								break;
							}
					}
				}

				if (gc.AsClient)
					LogManager.LogInfo("Logged in as " + Username);

				LogManager.LogInfo("Initializing verbs...");
				Verbs.Add(',', () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);
				Verbs.Add('`', () => RenderManager.DebugInformation = !RenderManager.DebugInformation);

				LogManager.LogInfo("Initializing internal scripts...");
				CurrentRoot = new DataModel(this);

				ProhibitProcessing = gc.ProhibitProcessing;
				ProhibitScripts = gc.ProhibitScripts;

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
					CurrentRoot.GetService<Debris>();
				}

				var rs = CurrentRoot.GetService<RunService>();
				var cg = CurrentRoot.GetService<CoreGui>();
				rs.Parent = CurrentRoot;
				cg.Parent = CurrentRoot;

				LuaRuntime.Setup(this, CurrentRoot);

				LogManager.LogInfo("Initializing user interface...");
				SetupCoreGui();

				LogManager.LogInfo("Initializing PhysicsManager...");
				PhysicsManager = new(this);

				CustomFlags = gc.CustomFlags;
				LogManager.LogInfo("Initializing RenderManager...");
				RenderManager = new(this, gc.SkipWindowCreation, !gc.DoNotRenderAtAll, gc.VersionMargin);

				if (NetworkManager.IsClient)
				{
					CurrentRoot.GetService<CoreGui>().ShowTeleportGui("", "", -1, -1);
					QueuedTeleportAddress = rbxlinit;
				}
				if (NetworkManager.IsServer)
				{
					AddedInstance += x =>
					{
						if (NetworkManager.Server == null) return;

						if (x.GetType().GetCustomAttribute<NotReplicatedAttribute>() == null)
							NetworkManager.AddReplication(x, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_NEWINST);
					};
				}
				servercallback(rbxlinit, this);
			}
			catch (Exception ex)
			{
				LogManager.LogError("A fatal error had occurred during NetBlox initialization! " + ex.GetType() + ", msg: " + ex.Message + ", stacktrace: " + ex.StackTrace);
				Environment.Exit(ex.GetHashCode());
				for (;;); // perhaps platform we're running on does not support exiting.
			}
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
		public void LoadDefault()
		{
			LogManager.LogInfo("Loading default place...");

			Workspace ws = CurrentRoot.GetService<Workspace>();
			ReplicatedStorage rs = CurrentRoot.GetService<ReplicatedStorage>();
			ReplicatedFirst ri = CurrentRoot.GetService<ReplicatedFirst>();
			Players pl = CurrentRoot.GetService<Players>();
			LocalScript ls = new(this);

			ws.ZoomToExtents();
			ws.Parent = CurrentRoot;

			Part part = new(this)
			{
				Parent = ws,
				Color = Color.DarkGreen,
				Position = new(0, -4.5f, 0),
				Size = new(32, 2, 32),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};

			new Part(this)
			{
				Parent = ws,
				Color = Color.DarkBlue,
				Position = new(0, -3f, 0),
				Size = new(1, 2, 1),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};
			new Part(this)
			{
				Parent = ws,
				Color = Color.DarkBlue,
				Position = new(-1, -3f, 0),
				Size = new(1, 2, 1),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};
			new Part(this)
			{
				Parent = ws,
				Color = Color.Red,
				Position = new(-0.5f, -1f, 0),
				Size = new(2, 2, 1),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};
			new Part(this)
			{
				Parent = ws,
				Color = Color.Yellow,
				Position = new(-2f, -1f, 0),
				Size = new(1, 2, 1),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};
			new Part(this)
			{
				Parent = ws,
				Color = Color.Yellow,
				Position = new(1f, -1f, 0),
				Size = new(1, 2, 1),
				TopSurface = SurfaceType.Studs,
				Anchored = true
			};

			ls.Parent = ri;
			ls.Source = "print(\"HIIIIII\"); printidentity();";

			rs.Parent = CurrentRoot;
			ri.Parent = CurrentRoot;
			pl.Parent = CurrentRoot;

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
			if (inst != null)
			{ // i was outsmarted
				if (inst.DestroyAt < DateTime.Now)
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
