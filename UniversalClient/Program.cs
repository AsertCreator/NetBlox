using NetBlox.Instances.Services;
using Raylib_cs;
using System.Net;

namespace NetBlox.Client
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			var v = Rlgl.GetVersion();
			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif

			LogManager.LogInfo($"NetBlox Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];

				gm.NetworkManager.ClientReplicator = Task.Run(async delegate ()
				{
					try
					{
						gm.NetworkManager.ConnectToServer(IPAddress.Parse(xo));
						return new object();
					}
					catch (Exception ex)
					{
						gm.RenderManager.Status = "Could not connect to the server: " + ex.Message;
						return new();
					}
				}).AsCancellable(gm.NetworkManager.ClientReplicatorCanceller.Token);
			};

			GameManager cg = null!;
			cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client"
			}, 
			args, (x, y) => { });
			cg.MainManager = true;
			AppManager.SetRenderTarget(cg);
			AppManager.Start();

			return 0;
		}
	}
}
