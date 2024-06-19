using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using Raylib_cs;
using System.Net;

namespace UniversalDuoHost
{
	internal static class Program
	{
		internal static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox DuoHost ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			LogManager.LogInfo("Initializing server...");

			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

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
					Color = Color.DarkGreen,
					Position = new(0, -4.5f, 0),
					Size = new(2048, 2, 2048),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};

				new Part(gm)
				{
					Parent = ws,
					Color = Color.DarkBlue,
					Position = new(0, -3f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Color.DarkBlue,
					Position = new(-1, -3f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Color.Red,
					Position = new(-0.5f, -1f, 0),
					Size = new(2, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
				{
					Parent = ws,
					Color = Color.Yellow,
					Position = new(-2f, -1f, 0),
					Size = new(1, 2, 1),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};
				new Part(gm)
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
					GameName = "NetBlox Client (duohosted)"
				},
				args, (x, y) => { });
				cg.MainManager = true;
				AppManager.SetRenderTarget(cg);
			});
			AppManager.Start();
			return 0;
		}
	}
}
