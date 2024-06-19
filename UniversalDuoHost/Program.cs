using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using Raylib_CsLo;
using System.Net;

namespace UniversalDuoHost
{
	internal static class Program
	{
		internal static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox DuoHost ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_NONE);

			var v = (rlGlVersion)RlGl.rlGetVersion();
			if (v == rlGlVersion.RL_OPENGL_11 || v == rlGlVersion.RL_OPENGL_21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

			bool running = true;
			bool runclient = true;
			bool runserver = true;

			Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_MSAA_4X_HINT);
			Raylib.InitWindow(1600, 900, "netblox");
			Raylib.SetTargetFPS(AppManager.PreferredFPS);
			Raylib.SetExitKey(KeyboardKey.KEY_NULL);

			while (running)
			{
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Raylib.WHITE);
				RayGui.GuiSetFont(ResourceManager.GetFont(AppManager.ContentFolder + "fonts/arialbd.ttf"));
				RayGui.GuiSetStyle((int)GuiControl.DEFAULT, (int)GuiDefaultProperty.TEXT_SIZE, 22);
				RayGui.GuiSetIconScale(2);

				RayGui.GuiLabel(new Rectangle(10, 10 + 25 * 0, 1600, 16), "Setup DuoHost:");
				runclient = RayGui.GuiCheckBox(new Rectangle(10, 10 + 25 * 1, 16, 16), "Run network client", runclient);
				runserver = RayGui.GuiCheckBox(new Rectangle(10, 10 + 25 * 2, 16, 16), "Run network server", runserver);

				RayGui.GuiLabel(new Rectangle(10, 10 + 25 * 4, 1600, 16), "Content folder: " + AppManager.ContentFolder);
				RayGui.GuiLabel(new Rectangle(10, 10 + 25 * 5, 1600, 16), "Library folder: " + AppManager.LibraryFolder);

				if (RayGui.GuiButton(new Rectangle(10, Raylib.GetScreenHeight() - 30 - 10, 200, 30), "#131#Start"))
					running = false;

				Raylib.EndDrawing();
			}

			LogManager.LogInfo("Initializing server...");

			var g = AppManager.CreateGame(new()
			{
				AsServer = true,
				DoNotRenderAtAll = true,
				SkipWindowCreation = true,
				GameName = "NetBlox Server (duohosted)"
			}, args, (x, gm) =>
			{
				Workspace ws = gm.CurrentRoot.GetService<Workspace>();
				ReplicatedStorage rs = gm.CurrentRoot.GetService<ReplicatedStorage>();
				ReplicatedFirst ri = gm.CurrentRoot.GetService<ReplicatedFirst>();
				Players pl = gm.CurrentRoot.GetService<Players>();
				LocalScript ls = new(gm);

				ws.ZoomToExtents();
				ws.Parent = gm.CurrentRoot;

				Part part = new(gm)
				{
					Parent = ws,
					Color = Raylib.DARKGREEN,
					Position = new(0, -4.5f, 0),
					Size = new(32, 2, 32),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};

				new Part(gm)
				{
					Parent = ws,
					Color = Raylib.DARKBLUE,
					Position = new(0, -3f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Raylib.DARKBLUE,
					Position = new(-1, -3f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Raylib.RED,
					Position = new(-0.5f, -1f, 0),
					Size = new(2, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Raylib.YELLOW,
					Position = new(-2f, -1f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Raylib.YELLOW,
					Position = new(1f, -1f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};

				ls.Parent = ri;
				ls.Source = "print(\"HIIIIII\"); printidentity();";

				rs.Parent = gm.CurrentRoot;
				ri.Parent = gm.CurrentRoot;
				pl.Parent = gm.CurrentRoot;

				gm.CurrentIdentity.MaxPlayerCount = 8;
				gm.CurrentIdentity.PlaceName = "Default Place";
				gm.CurrentIdentity.UniverseName = "NetBlox Defaults";
				gm.CurrentIdentity.Author = "The Lord";
				gm.CurrentIdentity.PlaceID = 47384;
				gm.CurrentIdentity.UniverseID = 47384;

				gm.CurrentRoot.Name = gm.CurrentIdentity.PlaceName;

				if (runserver)
					Task.Run(gm.NetworkManager.StartServer);
#if _WINDOWS
				AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif
				LogManager.LogInfo("Initializing client...");
				PlatformService.QueuedTeleport = (xo) =>
				{
					var gmc = AppManager.GameManagers[0];

					gmc.NetworkManager.ClientReplicator = Task.Run(async delegate ()
					{
						try
						{
							if (runclient)
								gmc.NetworkManager.ConnectToServer(IPAddress.Loopback);
							return new object();
						}
						catch (Exception ex)
						{
							gmc.RenderManager.Status = "Could not connect to the server: " + ex.Message;
							return new();
						}
					}).AsCancellable(gmc.NetworkManager.ClientReplicatorCanceller.Token);
				};

				GameManager cg = AppManager.CreateGame(new()
				{
					AsClient = true,
					SkipWindowCreation = true,
					GameName = "NetBlox Client (duohosted)"
				},
				args, (x, y) => { });
				cg.MainManager = true;
				AppManager.SetRenderTarget(cg);
			});
			AppManager.LoadFastFlags(args);
			AppManager.Start();
			return 0;
		}
	}
}
