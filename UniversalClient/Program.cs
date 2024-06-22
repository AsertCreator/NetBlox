using NetBlox.Instances.Services;
using Raylib_CsLo;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace NetBlox.Client
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_NONE);

			var v = (rlGlVersion)RlGl.rlGetVersion();
			if (v == rlGlVersion.RL_OPENGL_11 || v == rlGlVersion.RL_OPENGL_21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif

			if (args.Length == 1 && args[0] == "check")
			{
				return 0;
			}
			if (args.Length == 0)
			{
				LogManager.LogInfo("No arguments given, redirecting to Public Service's website...");
				if (!File.Exists("./ReferenceData.json"))
					return 1;
				Process.Start(new ProcessStartInfo()
				{
					FileName = JsonSerializer.Deserialize<Dictionary<string, string>>(
						File.ReadAllText("./ReferenceData.json"))!["PublicServiceAddress"],
					UseShellExecute = true
				});
				return 0;
			}

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
			AppManager.LoadFastFlags(args);
			AppManager.SetRenderTarget(cg);
			AppManager.Start();

			return 0;
		}
	}
}
