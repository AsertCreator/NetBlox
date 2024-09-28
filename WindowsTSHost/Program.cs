using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using Raylib_cs;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace WindowsTSHost
{
	internal static class Program
	{
		internal static string[] ConsoleArguments;

		[STAThread]
		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Team Sandbox ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			ConsoleArguments = args;

			AppManager.PlatformOpenBrowser = x =>
			{
				ProcessStartInfo psi = new()
				{
					FileName = x,
					UseShellExecute = true
				};
				Process.Start(psi);
			};
#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif

			if (args.Length > 0 && args[0] == "base64")
			{
				string bas64str = args[1];
				byte[] base64 = Encoding.UTF8.GetBytes(bas64str);
				Base64.DecodeFromUtf8InPlace(base64, out int bytes);
				string argstr = Encoding.UTF8.GetString(base64);
				args = argstr.Split(' ');
			}

			if (args.Length == 1 && args[0] == "check")
			{
				Console.WriteLine(NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
				return 0;
			}

			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				Environment.Exit(1);
			}

			LogManager.LogInfo("Starting TSH application...");
			GameManager game = CreateAppGame();

			AppManager.SetRenderTarget(game);
			AppManager.Start();

			return 0;
		}
		public static GameManager CreateAppGame()
		{
			PlatformService.QueuedTeleport = (xo) =>
			{
				var gm = AppManager.GameManagers[0];
				var ss = gm.CurrentRoot.GetService<SandboxService>();
				ss.StartTeamSandboxTitle();
			};
			GameManager cg = null!;
			cg = AppManager.CreateGame(new()
			{
				AsClient = true,
				GameName = "NTS Title"
			},
			["-cs", "{}"], (x) => x.CurrentRoot.IsApplication = true);
			cg.MainManager = true;
			return cg;
		}
	}
}
