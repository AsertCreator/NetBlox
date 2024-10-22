using NetBlox.Instances;
using NetBlox.Instances.Services;
using Raylib_cs;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NetBlox.Client
{
	public static class Program
	{
		public static string[] ConsoleArguments;

		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			ConsoleArguments = args;

			AppManager.PlatformOpenBrowser = x =>
			{
				ProcessStartInfo psi = new();
				psi.FileName = x;
				psi.UseShellExecute = true;
				Process.Start(psi);
			};
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
				Console.WriteLine(Common.Version.VersionMajor + "." + Common.Version.VersionMinor + "." + Common.Version.VersionPatch);
				return 0;
			}

			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				Environment.Exit(1);
			}

			GameManager game;

			if (args.Length == 0)
			{
				LogManager.LogInfo("No arguments, starting app...");
				game = CreateAppGame();
			}
			else
			{
				LogManager.LogInfo("Starting general game...");
				game = CreateGeneralGame(args);
			}

			AppManager.SetRenderTarget(game);
			AppManager.Start();

			return 0;
		}
		public static GameManager CreateAppGame()
		{
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];
				var coregui = gm.CurrentRoot.GetService<CoreGui>();
				var sctx = gm.CurrentRoot.GetService<ScriptContext>();
				sctx.AddCoreScriptLocal("CoreScripts/Application", coregui.FindFirstChild("RobloxGui")!);
			};
			GameManager cg = null!;
			cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client"
			},
			["-cs", "{}"], (x) => x.CurrentRoot.IsApplication = true);
			cg.MainManager = true;
			return cg;
		}
		public static GameManager CreateGeneralGame(string[] args)
		{
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];

				gm.NetworkManager.ClientReplicator = Task.Run(delegate ()
				{
					try
					{
						gm.NetworkManager.ConnectToServer(IPAddress.Parse(xo));
						return Task.FromResult(new object());
					}
					catch (Exception ex)
					{
						gm.RenderManager.Status = "Could not connect to the server: " + ex.Message;
						return Task.FromResult<object>(new());
					}
				}).AsCancellable(gm.NetworkManager.ClientReplicatorCanceller.Token);
			};
			GameManager cg = null!;
			cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NetBlox Client"
			},
			args, (x) => x.RenderManager.WhiteOut = true);
			cg.MainManager = true;
			return cg;
		}
	}
}
