using NetBlox;
using NetBlox.Instances.Services;
using Raylib_cs;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Text;

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
					x.LoadDefault(1);
				else
					x.CurrentRoot.Load(rbxlfile);

				x.CurrentIdentity.PlaceName = "Personal Place";
				x.CurrentIdentity.UniverseName = "NetBlox Defaults";
				x.CurrentIdentity.Author = "The Lord";
				x.CurrentIdentity.MaxPlayerCount = 5;
				x.CurrentRoot.Name = x.CurrentRoot.Name;

				Task.Run(x.NetworkManager.StartServer);
			});
		}
		internal static GameManager CreateClient()
		{
			PlatformService.QueuedTeleport = (xo) =>
			{
				CurrentClient.ConnectLoopback();
			};

			CurrentClient = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client (duohosted)"
			},
			["-cs", "{\"a\":\"http://localhost:80/\",\"b\":\"NetBlox Development\",\"e\":true,\"g\":\"127.0.0.1\"}"], (x) => { });
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

#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif

			if (args.Length > 0 && args[0] == "base64")
			{
				string bas64str = args[1];
				byte[] base64 = Encoding.UTF8.GetBytes(bas64str);
				int bytes = 0;
				Base64.DecodeFromUtf8InPlace(base64, out bytes);
				string argstr = Encoding.UTF8.GetString(base64);
				args = argstr.Split(' ');
			}

			if (args.Length == 1 && args[0] == "check")
			{
				Console.WriteLine(NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
				return 0;
			}

			AppManager.PlatformOpenBrowser = x =>
			{
				ProcessStartInfo psi = new();
				psi.FileName = x;
				psi.UseShellExecute = true;
				Process.Start(psi);
			};

			LogManager.LogInfo("Initializing server...");

			CurrentServer = CreateServer(25570);
			CurrentClient = CreateClient();
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
