global using Color = Raylib_cs.Color;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.GUIs;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Network;
using Raylib_cs;
using System.Diagnostics;
using NetBlox.Instances.Parts;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);

	/// <summary>
	/// Represents a NetBlox game. Believe it or not, but one NetBlox process can run multiple games at once (demonstrated clearly
	/// in the DuoHost project)
	/// </summary>
	public class GameManager
	{
		/// <summary>
		/// Contains all <seealso cref="Instance"/>s belonging to this <seealso cref="GameManager"/> instance
		/// <br/><br/>
		/// You can in theory change an Instance's owner GameManager, but it's like really wrong, because they should have 0
		/// references to any internal structures or other Instances for a correct transfer and it's like painful to do. 
		/// Better serialize the Instance and deserialize it in the target GameManager.
		/// </summary>
		public Dictionary<Guid, Instance> AllInstances = [];
		/// <summary>
		/// Upon clicking on these keys these <seealso cref="Action"/>s will be invoked
		/// </summary>
		public Dictionary<KeyboardKey, Action> Verbs = [];

		/// <summary>
		/// Describes the "network" identity of the place this GameManager is in. Includes the place name, id, author etc
		/// </summary>
		public NetworkIdentity CurrentIdentity = new();

		/// <summary>
		/// Contains the <seealso cref="NetBlox.RenderManager"/> object associated with this GameManager instance.
		/// </summary>
		public RenderManager RenderManager;
		public PhysicsManager PhysicsManager;
		public NetworkManager NetworkManager;
		public DataModel CurrentRoot = null!;
		public RunService? CurrentRunService;
		public ProfileManager CurrentProfile = new();

		public ConfigFlags CustomFlags;

		/// <summary>
		/// Indicates whether the current environment is a studio environment.
		/// </summary>
		public bool IsStudio = false;
		/// <summary>
		/// Indicates whether the game processor should process this instance.
		/// </summary>
		public bool IsRunning
		{
			get => isRunning;
			set
			{
				if (isRunning != value)
					LogManager.LogInfo("Setting IsRunning for game manager \"" + GameName + "\" to " + value);
				isRunning = value;
			}
		}
		/// <summary>
		/// Indicates whether this <seealso cref="GameManager"/> instance is in the process of shutting down.
		/// </summary>
		public bool ShuttingDown = false;
		/// <summary>
		/// Pauses running of scripts belonging to this <seealso cref="GameManager"/> instance.
		/// </summary>
		public bool ProhibitScripts = false;
		/// <summary>
		/// If this <seealso cref="GameManager"/> instance has this field set to <seealso cref="true"/> then upon
		/// shutting it down the whole application will be shut down.
		/// </summary>
		public bool MainManager = false;
		/// <summary>
		/// Indicates whether usage of public service web APIs is allowed in this <seealso cref="GameManager"/> instance.
		/// </summary>
		public bool UsePublicService = false;
		/// <summary>
		/// Indicates whether this instance of <seealso cref="GameManager"/> should process incoming serverbound network 
		/// packets that create new instances. Doesn't actually matter if it's false lol im not implementing the else behavior
		/// </summary>
		public bool FilteringEnabled = true;
		/// <summary>
		/// Indicates whether the replication loop is paused.
		/// </summary>
		public bool PauseReplication = false;
		/// <summary>
		/// The address of the server that the client should teleport to upon the call to <seealso cref="PlatformService.BeginQueuedTeleport"/>
		/// </summary>
		public string QueuedTeleportAddress = "";
		/// <summary>
		/// The name of this <seealso cref="GameManager"/> instance.
		/// </summary>
		public string GameName = "";

		/// <summary>
		/// uhhhh something i forgot
		/// </summary>
		public int PropertyReplicationRate = 20;

		/// <summary>
		/// The UTC date and time when this instance of <seealso cref="GameManager"/> was created.
		/// </summary>
		public DateTime TimeOfCreation = DateTime.UtcNow;

		/// <summary>
		/// Contains the client startup info properly parsed. This should be null on servers. If this is null on a client
		/// then you're doing something very early and should do it later.
		/// </summary>
		public ClientStartupInfo? ClientStartupInfo;
		/// <summary>
		/// Contains the server startup info properly parsed. This should be null on clients. If this is null on a server
		/// then you're doing something very early and should do it later.
		/// </summary>
		public ServerStartupInfo? ServerStartupInfo;

		/// <summary>
		/// Contains all loaded ModuleScripts and their evaluated Lua API table module things i dunno.
		/// </summary>
		public Dictionary<ModuleScript, DynValue> LoadedModules = new();
		/// <summary>
		/// The main Lua environment that all scripts in this <seealso cref="GameManager"/> instance run in.
		/// </summary>
		public MoonSharp.Interpreter.Script MainEnvironment = null!;

		/// <summary>
		/// Returns the username of the currently logged in player. Supported even on guest sessions.
		/// </summary>
		public string Username => CurrentProfile != null ? CurrentProfile.Username : "Guest";

		/// <summary>
		/// Raised when this instance of <seealso cref="GameManager"/> is shutting down.
		/// </summary>
		public event EventHandler? ShutdownEvent;

		private bool isRunning = false;

		public GameManager(GameConfiguration gc, string[] args, Action<GameManager> loadcallback, Action<DataModel>? dmc = null)
		{
			GameName = gc.GameName;

			var oldgm = AppManager.CurrentGameManager;
			AppManager.CurrentGameManager = this;

			try
			{
				LogManager.LogInfo("Initializing NetBlox...");

				if (args.Length == 0)
					throw new InvalidOperationException("Cannot initialize NetBlox with no arguments!");

				string? csdata = args[args.ToList().IndexOf("-cs") + 1].Replace("^^", "\"");
				string? ssdata = args[args.ToList().IndexOf("-ss") + 1].Replace("^^", "\"");

				try
				{
					if (gc.AsClient)
						ClientStartupInfo = csdata != null ? SerializationManager.DeserializeJson<ClientStartupInfo>(csdata) : null;
					if (gc.AsServer)
						ServerStartupInfo = ssdata != null ? SerializationManager.DeserializeJson<ServerStartupInfo>(ssdata) : null;

					if (ClientStartupInfo == null && gc.AsClient)
						throw new Exception("Missing client startup info");
					if (ServerStartupInfo == null && gc.AsServer)
						throw new Exception("Missing server startup info");

					AppManager.PublicServiceAPI =
						gc.AsServer ?
						(ServerStartupInfo ?? throw new Exception()).PublicServiceAPI :
						(ClientStartupInfo ?? throw new Exception()).PublicServiceAPI;
				}
				catch
				{
					LogManager.LogError("Could not parse startup information: " + csdata + ssdata);
					Environment.Exit(1);
				}

				NetworkManager = new(this, gc.AsServer, gc.AsClient);
				CurrentIdentity.Reset();
				IsStudio = gc.AsStudio;

				if (gc.AsClient)
				{
					Debug.Assert(ClientStartupInfo != null);

					if (AppManager.GetFastFlag("FFlagCompletelySkipLoggingIn", false))
					{
						LogManager.LogWarn("Skipping calling home, authorizing as guest...");
						CurrentProfile.LoginAsGuest();
					}
					else
					{
						var user = ClientStartupInfo.Username;
						var hash = ClientStartupInfo.PasswordHash;
						Guid? token = CurrentProfile.LoginAsync(user, hash).WaitAndGetResult();
						if (token == null)
							CurrentProfile.LoginAsGuest();
					}

					LogManager.LogInfo("Logged in as " + Username);
				}

				ProhibitScripts = gc.ProhibitScripts;

				LogManager.LogInfo("Initializing PhysicsManager...");
				PhysicsManager = new(this);

				CustomFlags = gc.CustomFlags;
				LogManager.LogInfo("Initializing RenderManager...");
				RenderManager = new(this, gc.SkipWindowCreation, !gc.DoNotRenderAtAll);

				LogManager.LogInfo("Initializing verbs...");
				Verbs.Add(KeyboardKey.Comma, () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);
				Verbs.Add(KeyboardKey.Apostrophe, () => RenderManager.DebugInformation = !RenderManager.DebugInformation);
				Verbs.Add(KeyboardKey.F3, () =>
				{
					var coregui = CurrentRoot.GetService<CoreGui>(true);
					if (coregui != null)
						coregui.TakeScreenshot();
				});
				Verbs.Add(KeyboardKey.F4, () =>
				{
					RenderManager.DoRenderDebugCharts = !RenderManager.DoRenderDebugCharts;
				});
				Verbs.Add(KeyboardKey.F5, () =>
				{
					RenderManager.DebugInformation = !RenderManager.DebugInformation;
				});
				Verbs.Add(KeyboardKey.F6, () =>
				{
					RenderManager.UnlimitFramerate = !RenderManager.UnlimitFramerate;
				});
				Verbs.Add(KeyboardKey.K, () =>
				{
					RenderManager.FrustumCullingPaused = !RenderManager.FrustumCullingPaused;
				});
				Verbs.Add(KeyboardKey.L, () =>
				{
					var light = CurrentRoot.GetService<Lighting>(true);
					if (light != null)
						light.SunLocality = !light.SunLocality;
				});

				Verbs.Add(KeyboardKey.LeftBracket, () =>
				{
					if (AppManager.PreferredFPS == 1)
						AppManager.PreferredFPS = 0;
					AppManager.PreferredFPS -= 5;
					if (AppManager.PreferredFPS == 0)
						AppManager.PreferredFPS = 1;
				});
				Verbs.Add(KeyboardKey.RightBracket, () =>
				{
					if (AppManager.PreferredFPS == 1)
						AppManager.PreferredFPS = 0;
					AppManager.PreferredFPS += 5;
					if (AppManager.PreferredFPS == 0)
						AppManager.PreferredFPS = 1;
				});

				// we dont want corescripts to run before engine is initialized

				CurrentRoot = new DataModel(this);
				if (dmc != null)
					dmc(CurrentRoot);

				var rs = CurrentRoot.GetService<RunService>();
				CurrentRunService = rs;

				LuaRuntime.Setup(this);

				if (NetworkManager.IsClient)
				{
					LogManager.LogInfo("Creating main client services...");
					CurrentRoot.GetService<SandboxService>();
					CurrentRoot.GetService<Debris>();

					LogManager.LogInfo("Initializing client CoreScripts...");
					SetupCoreGui();
				}
				if (NetworkManager.IsServer)
				{
					LogManager.LogInfo("Creating main server services...");
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

					LogManager.LogInfo("Initializing server CoreScripts...");
					SetupCoreGui();
				}

				var cg = CurrentRoot.GetService<CoreGui>();

				if (NetworkManager.IsClient)
				{
					cg.ShowTeleportGui("", "", -1, -1);
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
			finally
			{
				AppManager.CurrentGameManager = oldgm;
			}
		}
		public void SetupCoreGui()
		{
			CoreGui cg = CurrentRoot.GetService<CoreGui>();
			ScreenGui sg = new(this);
			sg.Name = "RobloxGui";
			sg.Parent = cg;

			// apparently roblox does not just load all corescritps on bulk.
			var scrurl = AppManager.ResolveUrlAsync("rbxasset://scripts/Modules/", false).WaitAndGetResult();
			string? ssurl;

			if (NetworkManager.IsServer)
			{
				LogManager.LogInfo("Resolving bootstrap CoreScript for ServerStarterScript...");
				ssurl = AppManager.ResolveUrlAsync("rbxasset://scripts/ServerStarterScript.lua", false).WaitAndGetResult();
			}
			else
			{
				LogManager.LogInfo("Resolving bootstrap CoreScript for StarterScript...");
				ssurl = AppManager.ResolveUrlAsync("rbxasset://scripts/StarterScript.lua", false).WaitAndGetResult();
			}

			if (!File.Exists(ssurl))
				throw new Exception("No StarterScript/ServerStarterScript found in the content directory!");

			var modulesFolder = new Folder(this);
			modulesFolder.Name = "Modules";
			modulesFolder.Parent = sg;
			var files = Directory.GetFiles(scrurl);

			for (int i = 0; i < files.Length; i++)
			{
				ModuleScript ms = new(this);
				ms.Name = Path.GetFileNameWithoutExtension(files[i]);
				ms.Source = File.ReadAllText(files[i]);
				ms.Parent = modulesFolder;
			}

			LogManager.LogInfo("Loaded " + files.Length + " CoreScript modules from the content directory");

			CoreScript ss = new(this);
			ss.Name = "StarterScript";
			ss.Source = File.ReadAllText(ssurl);
			ss.Parent = sg;
		}
		public Instance? TryGetService(ServiceType type)
		{
			byte[] craftedServiceGuidBuffer = BitConverter.GetBytes((int)type);
			if (craftedServiceGuidBuffer.Length < 16)
				Array.Resize(ref craftedServiceGuidBuffer, 16);
			Guid craftedServiceGuid = new Guid(craftedServiceGuidBuffer);
			return GetInstance(craftedServiceGuid);
		}
		public Instance CreateService(ServiceType type)
		{
			byte[] craftedServiceGuidBuffer = BitConverter.GetBytes((int)type);
			if (craftedServiceGuidBuffer.Length < 16)
				Array.Resize(ref craftedServiceGuidBuffer, 16);
			Guid craftedServiceGuid = new Guid(craftedServiceGuidBuffer);

			Instance inst = InstanceCreator.CreateServiceInstanceIfExists(type.ToString(), this);
			inst.Parent = CurrentRoot;
			inst.ChangeUniqueID(craftedServiceGuid);
			return inst;
		}
		public void RegisterService(Instance service, ServiceType type)
		{
			byte[] craftedServiceGuidBuffer = BitConverter.GetBytes((int)type);
			if (craftedServiceGuidBuffer.Length < 16)
				Array.Resize(ref craftedServiceGuidBuffer, 16);
			Guid craftedServiceGuid = new Guid(craftedServiceGuidBuffer);

			if (GetInstance(craftedServiceGuid) != null)
			{
				throw new Exception("Cannot create a second instance of a singleton Instance (" + type + ")");
			}

			service.Parent = CurrentRoot;
			service.ChangeUniqueID(craftedServiceGuid);
		}
		public void Shutdown()
		{
			LogManager.LogInfo($"Shutting down GameManager \"{GameName}\"...");
			ShuttingDown = true;
			ShutdownEvent?.Invoke(new(), new());
			AppManager.GameManagers.Remove(this);

			if (AppManager.CurrentRenderManager == RenderManager)
				AppManager.CurrentRenderManager = null;

			if (RenderManager != null)
				RenderManager.Unload();
			RenderManager = null;

			if (NetworkManager != null)
			{
				if (NetworkManager.IsClient)
				{
					NetworkManager.DisconnectFromServer("The client is closing");
				}
				else if (NetworkManager.IsServer)
				{
					NetworkManager.Clients.ForEach(x =>
					{
						x.KickOut("The server is closing");
					});
				}
			}

			TaskScheduler.ScheduleDelayedNamedJob("GameManagerShutdownJob", TimeSpan.FromSeconds(4), JobType.Miscellaneous, _ =>
			{
				Job[] belongingjobs = new Job[TaskScheduler.RunningJobs.Count];

				TaskScheduler.RunningJobs.CopyTo(belongingjobs);

				for (int i = 0; i < belongingjobs.Length; i++)
				{
					if (belongingjobs[i].ScriptJobContext.GameManager == this)
						TaskScheduler.Terminate(belongingjobs[i]);
				}

				return JobResult.NotCompleted;
			});

			if (MainManager)
			{
				AppManager.Shutdown();
			}
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
							Position = new(0, 5f, 0),
							Size = new(512, 2, 512),
							TopSurface = SurfaceType.Studs,
							Anchored = true
						};
						SpawnLocation sloc = new(this)
						{
							Parent = ws,
							Color3 = Color.Gray,
							Position = new(0, 6f, 0),
							Size = new(6, 1, 6),
							TopSurface = SurfaceType.Studs,
							Anchored = true
						};

						for (int k = 0; k < 7; k++)
						{
							for (int i = 0; i < 7; i++)
							{
								for (int j = 0; j < i; j++)
								{
									_ = new Part(this)
									{
										Parent = ws,
										Color3 = Raylib.Fade(Color.White, j / i * 0.9f + 0.1f),
										Position = new(k * 1.5f, 20 + j * 1.5f, i * 1.5f),
										Size = new(1, 1, 1),
										Anchored = false,
										TopSurface = SurfaceType.Studs,
										BottomSurface = SurfaceType.Studs,
										LeftSurface = SurfaceType.Studs,
										RightSurface = SurfaceType.Studs,
										FrontSurface = SurfaceType.Studs,
										BackSurface = SurfaceType.Studs,
									};
								}
							}
						}

						_ = new Part(this)
						{
							Parent = ws,
							Color3 = Color.White,
							Position = new(-10, 40, -10),
							Size = new(3, 40, 3),
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
						_ = new SpawnLocation(this)
						{
							Parent = ws,
							Position = new(0, -45f + 2, 0),
							TopSurface = SurfaceType.Studs
						};

						_ = new Part(this)
						{
							Parent = ws,
							Anchored = false,
							Color3 = Color.DarkBlue,
							Position = new(0, -3f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						_ = new Part(this)
						{
							Parent = ws,
							Anchored = false,
							Color3 = Color.DarkBlue,
							Position = new(-1, -3f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						_ = new Part(this)
						{
							Parent = ws,
							Anchored = false,
							Color3 = Color.Red,
							Position = new(-0.5f, -1f, 0),
							Size = new(2, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						_ = new Part(this)
						{
							Parent = ws,
							Anchored = false,
							Color3 = Color.Yellow,
							Position = new(-2f, -1f, 0),
							Size = new(1, 2, 1),
							TopSurface = SurfaceType.Studs
						};
						_ = new Part(this)
						{
							Parent = ws,
							Anchored = false,
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
			CurrentIdentity.PlaceName = "";
			CurrentIdentity.UniverseName = "";
			CurrentIdentity.Author = "";
			CurrentIdentity.PlaceID = 0;
			CurrentIdentity.UniverseID = 0;

			CurrentRoot.Name = CurrentIdentity.PlaceName;
		}
		public Instance? GetInstance(Guid id)
		{
			lock (AllInstances)
			{
				if (AllInstances.TryGetValue(id, out Instance value))
					return value;
				return null;
			}
		}
		public void ProcessInstance(Instance inst)
		{
			try
			{
				if (inst != null)
				{
					if (inst.WasDestroyed)
						return;
					if (inst.DestroyAt < DateTime.UtcNow)
					{
						inst.Destroy();
						return;
					}

					inst.Process();

					Instance[] children = new Instance[inst.Children.Count];
					inst.Children.CopyTo(children);

					for (int i = 0; i < children.Length; i++)
					{
						if (children[i] == null)
							continue;
						if (children[i].WasDestroyed)
							continue;
						ProcessInstance(children[i]);
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.LogWarn("An exception occurred during processing " + inst.GetFullName() + ", " + ex.GetType() + ", msg: " + ex.Message);
			}
		}
		public override string ToString() => "GM-" + GameName;
	}
}
