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

			LogManager.LogInfo($"NetBlox Client ({GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}) is running...");
			PlatformService.QueuedTeleport = () =>
			{
				NetworkManager.ConnectToServer(IPAddress.Parse(xo));
				Task.Run(() =>
				{
					Console.WriteLine("NetBlox Console is running (enter Lua code to run it)");
					while (!GameManager.ShuttingDown)
					{
						Console.Write(">>> ");
						var c = Console.ReadLine();
						LuaRuntime.Execute(c, 8, null, GameManager.CurrentRoot);
					}
				});
			};
			GameManager.Start(true, false, true, args, x => xo = x);

			return 0;
		}
	}
}