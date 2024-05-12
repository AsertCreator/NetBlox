using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Net;

namespace NetBlox.Client
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var xo = "";
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
		}
	}
}