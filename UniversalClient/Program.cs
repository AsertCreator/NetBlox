using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Net;
using System.Runtime.InteropServices;

namespace NetBlox.Client
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var xo = "";
			var v = Rlgl.GetVersion();

			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

			LogManager.LogInfo($"NetBlox Client ({SharedData.VersionMajor}.{SharedData.VersionMinor}.{SharedData.VersionPatch}) is running...");
			PlatformService.QueuedTeleport = () =>
			{
				var gm = SharedData.GameManagers[0];
				gm.NetworkManager.ConnectToServer(IPAddress.Parse(xo));
				Task.Run(() =>
				{
					Console.WriteLine("NetBlox Console is running (enter Lua code to run it)");
					while (!gm.ShuttingDown)
					{
						Console.Write(">>> ");
						var c = Console.ReadLine();
						LuaRuntime.Execute(c, 8, gm, null, gm.CurrentRoot);
					}
				});
			};
			SharedData.CreateGame("NetBlox Client", true, false, true, args, (x, y) => { });

			return 0;
		}
	}
}
