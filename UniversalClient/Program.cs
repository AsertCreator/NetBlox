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
		public static int Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Client ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");

			Raylib.SetTraceLogLevel(TraceLogLevel.None);

			var v = Rlgl.GetVersion();
			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				Environment.Exit(1);
			}

#if _WINDOWS
			AppManager.LibraryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NetBlox").Replace("\\", "/");
#endif
			if (args[0] == "base64")
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
			args, (x) => { });
			cg.MainManager = true;
			AppManager.PlatformOpenBrowser = x =>
			{
				ProcessStartInfo psi = new();
				psi.FileName = x;
				psi.UseShellExecute = true;
				Process.Start(psi);
			};
			AppManager.SetRenderTarget(cg);
			AppManager.Start();

			return 0;
		}
	}
}
