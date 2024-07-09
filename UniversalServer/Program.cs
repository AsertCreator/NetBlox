using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Server;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Server
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Server ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			/*GameManager.Start(false, true, false, args, x =>
			{
				DataModel dm = new();
				RbxlParser.Load(x, Root);

				Root = dm;

				LuaRuntime.Setup(Root, false);

				NetworkManager.StartServer();

				GameManager.IsRunning = true;
			});
			return;*/
			var g = AppManager.CreateGame(new()
			{
				AsServer = true,
				DoNotRenderAtAll = true,
				SkipWindowCreation = true,
				GameName = "NetBlox Server"
			}, args, (x) =>
			{
				x.LoadDefault(1);
				Task.Run(x.NetworkManager.StartServer);
			});
			g.MainManager = true;
			AppManager.SetRenderTarget(g);
			AppManager.Start();
		}
	}
}
