using MoonSharp.Interpreter;
using NetBlox.Runtime;
using Raylib_cs;
using System.Diagnostics;
using System.IO.Pipes;

namespace NetBlox.Instances.Services
{
	public class PlatformService : Instance
	{
		public static Action<string> QueuedTeleport = (xo) => { throw new Exception("NetBlox died!"); };
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsStudio => GameManager.IsStudio;
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsOffline => Profile.IsOffline;
		[Lua([Security.Capability.CoreSecurity])]
		public bool LoggedIn => Profile.LastLogin != null;
		public override Security.Capability[] RequiredCapabilities => [Security.Capability.CoreSecurity];

		public PlatformService(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.CoreSecurity])]
		public void BeginQueuedTeleport() => QueuedTeleport(GameManager.QueuedTeleportAddress);
		[Lua([Security.Capability.CoreSecurity])]
		public string[] GetConsoleArguments() => Environment.GetCommandLineArgs();
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsClient() => GameManager.NetworkManager.IsClient;
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsServer() => GameManager.NetworkManager.IsServer;
		[Lua([Security.Capability.CoreSecurity])]
		public void SetRenderFlag(string flag)
		{
			var type = typeof(ConfigFlags);
			var conf = (ConfigFlags)(type.GetEnumValues() as uint[])[Array.FindIndex(type.GetEnumNames(), x => x == flag)];
			// help
			Raylib.SetConfigFlags(conf);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void SetPreference(string key, string val) => AppManager.SetPreference(key, val);
		[Lua([Security.Capability.CoreSecurity])]
		public string GetPreference(string key) => AppManager.GetPreference(key);
		[Lua([Security.Capability.CoreSecurity])]
		public void OpenBrowserWindow(string url)
		{
			ProcessStartInfo psi = new();
			psi.FileName = url;
			psi.UseShellExecute = true;
			System.Diagnostics.Process.Start(psi);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string FormatVersion() => $"{GameManager.ManagerName}, v{AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}";
		[Lua([Security.Capability.CoreSecurity])]
		public void EnableStatusPipe()
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new Exception("Cannot start status pipe in client");

			Process pr = System.Diagnostics.Process.GetCurrentProcess();

			Task.Run(() =>
			{
				while (GameManager.IsRunning)
				{
					using (NamedPipeServerStream ss = new("netblox.index" + pr.Id, PipeDirection.Out))
					{
						ss.WaitForConnection();
						using (StreamWriter sw = new StreamWriter(ss))
						{
							sw.WriteLine(GameManager.CurrentIdentity.PlaceName);
							sw.WriteLine(GameManager.CurrentIdentity.UniverseName);
							sw.WriteLine(GameManager.CurrentIdentity.Author);
							sw.WriteLine("");
							sw.WriteLine(GameManager.CurrentIdentity.PlaceID);
							sw.WriteLine(GameManager.CurrentIdentity.PlaceID);
							sw.WriteLine(-1);
							sw.WriteLine(GameManager.NetworkManager.Clients.Count);
							sw.WriteLine(GameManager.CurrentIdentity.MaxPlayerCount);

							sw.Flush();
						}
					}
				}
			});
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void EnableRctlPipe()
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new Exception("Cannot start remote control pipe in client");

			Process pr = System.Diagnostics.Process.GetCurrentProcess();

			Task.Run(() =>
			{
				while (GameManager.IsRunning)
				{
					using (NamedPipeServerStream ss = new("netblox.rctl" + pr.Id, PipeDirection.In))
					{
						ss.WaitForConnection();
						using (StreamReader sw = new StreamReader(ss))
						{
							string what = sw.ReadLine()!;
							switch (what)
							{
								case "runlua":
									LuaRuntime.Execute(sw.ReadToEnd(), 8, GameManager, null);
									break;
								case "kickall":
									var pl = Root.GetService<Players>();
									pl.KickAll(sw.ReadToEnd());
									break;
								case "sysmsg":
									throw new NotImplementedException("no");
							}
						}
					}
				}
			});
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PlatformService) == classname) return true;
			return base.IsA(classname);
		}
	}
}
