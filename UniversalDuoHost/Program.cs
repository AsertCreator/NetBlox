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

			// Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}
			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			LogManager.LogInfo("Initializing server...");

			var g = AppManager.CreateGame(new()
			{
				AsServer = true,
				DoNotRenderAtAll = true,
				SkipWindowCreation = true,
				GameName = "NetBlox Server (duohosted)"
			}, ["-ss", "{}"], (x) =>
			{
				x.LoadDefault();
				Task.Run(x.NetworkManager.StartServer);
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
				["-cs", "{\"e\":true,\"g\":\"127.0.0.1\"}"], (x) => { });
				cg.MainManager = true;
				AppManager.SetRenderTarget(cg);
			});
			AppManager.Start();
			return 0;
		}
	}
}
