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
		internal static GameManager CurrentClient;
		internal static GameManager CurrentServer;

		internal static GameManager CreateServer(int port, string? rbxlfile = null)
		{
			return AppManager.CreateGame(new()
			{
				AsServer = true,
				DoNotRenderAtAll = true,
				SkipWindowCreation = true,
				GameName = "NetBlox Server (duohosted)"
			}, ["-ss", "{\"f\":" + port + "}"], (x) =>
			{
				if (rbxlfile == null)
					x.LoadDefault();
				else
				{
					x.CurrentRoot.Load(rbxlfile);
					x.CurrentIdentity.PlaceName = "Personal Place";
					x.CurrentIdentity.UniverseName = "NetBlox Defaults";
					x.CurrentIdentity.Author = "The Lord";
					x.CurrentIdentity.MaxPlayerCount = 5;
					x.CurrentRoot.Name = x.CurrentRoot.Name;
				}

				Task.Run(x.NetworkManager.StartServer);
			});
		}
		internal static GameManager CreateClient()
		{
			PlatformService.QueuedTeleport = (xo) =>
			{
			};

			CurrentClient = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client (duohosted)"
			},
			["-cs", "{\"e\":true,\"g\":\"127.0.0.1\"}"], (x) => { });
			CurrentClient.MainManager = true;
			AppManager.SetRenderTarget(CurrentClient);
			return CurrentClient;
		}
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

			CurrentServer = CreateServer(25570);
			CurrentClient = CreateClient();
			CurrentClient.ConnectLoopback();
			AppManager.Start();
			return 0;
		}
		internal static void ConnectLoopback(this GameManager gm)
		{
			gm.NetworkManager.ClientReplicator = Task.Run(delegate ()
			{
				try
				{
					gm.NetworkManager.ConnectToServer(IPAddress.Loopback);
					return Task.FromResult(new object());
				}
				catch (Exception ex)
				{
					gm.RenderManager.Status = "Could not connect to the server: " + ex.Message;
					return Task.FromResult<object>(new());
				}
			}).AsCancellable(gm.NetworkManager.ClientReplicatorCanceller.Token);
		}
	}
}
