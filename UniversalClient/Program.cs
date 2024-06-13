using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;

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
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox");
#endif

			LogManager.LogInfo($"NetBlox Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];

				gm.NetworkManager.ClientReplicator = Task.Run(async delegate ()
				{
					gm.NetworkManager.ConnectToServer(IPAddress.Parse(xo));
					return new object();
				}).AsCancellable(gm.NetworkManager.ClientReplicatorCanceller.Token);
			};
			var cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client"
			}, 
			args, (x, y) => { });
			cg.MainManager = true;
			Task.Run(() =>
			{
				Console.WriteLine("NetBlox Console is running (enter Lua code to run it)");
				while (!cg.ShuttingDown)
				{
					Console.Write(">>> ");
					var c = Console.ReadLine();
					LuaRuntime.Execute(c, 8, cg, null);
				}
			});
			AppManager.SetRenderTarget(cg);
			AppManager.Start();

			return 0;
		}
	}
}
