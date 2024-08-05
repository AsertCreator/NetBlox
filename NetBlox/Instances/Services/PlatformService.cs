using MoonSharp.Interpreter;
using NetBlox.Common;
using NetBlox.Runtime;
using Raylib_cs;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class PlatformService : Instance
	{
		public static Action<string> QueuedTeleport = (xo) => { throw new Exception("NetBlox died!"); };
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsStudio => GameManager.IsStudio;
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsOffline => GameManager.CurrentProfile.IsOffline;
		[Lua([Security.Capability.CoreSecurity])]
		public bool LoggedIn => GameManager.CurrentProfile.LastLogin != null;
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
			var conf = (ConfigFlags)((uint[])type.GetEnumValues())[Array.FindIndex(type.GetEnumNames(), x => x == flag)];
			// help
			Raylib.SetConfigFlags(conf);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void SetPreference(string key, string val) => AppManager.SetPreference(key, val);
		[Lua([Security.Capability.CoreSecurity])]
		public string GetPreference(string key) => AppManager.GetPreference(key);
		[Lua([Security.Capability.CoreSecurity])]
		public void OpenBrowserWindow(string url) => AppManager.PlatformOpenBrowser(url);
		[Lua([Security.Capability.CoreSecurity])]
		public string FormatVersion() => $"{GameManager.ManagerName}, v{AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}";
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
					using (NamedPipeServerStream ss = new("netblox.rctl" + pr.Id, PipeDirection.InOut))
					{
						try
						{
							ss.WaitForConnection();
							string str = ss.ReadToEnd();
							string[] blobs = str.Split('\n');
							string cmdblob = blobs[0];
							string argblob = blobs[1];

							using (StreamWriter sw = new(ss))
							{
								switch (cmdblob)
								{
									case "nb2-rctrl-kick":
										{
											Players plrs = Root.GetService<Players>();
											Instance[] plrc = plrs.GetChildren();
											long uid = long.Parse(argblob.Split(';')[0]);
											string msg = argblob.Split(';')[1];
											for (int i = 0; i < plrc.Length; i++)
											{
												Player plr = (Player)plrc[i];
												if (plr.UserId == uid)
												{
													plr.Kick(msg);
													break;
												}
											}
											sw.Write("");
											break;
										}
									case "nb2-rctrl-getuids":
										{
											Players plrs = Root.GetService<Players>();
											Instance[] plrc = plrs.GetChildren();

											sw.Write(string.Join(';', from x in plrc select ((Player)x).UserId));
											break;
										}
									case "nb2-rctrl-runlua":
										{
											LogManager.LogWarn("A Lua code is about to be run, originated from Public Service!");

											string luablob = str.Substring(str.IndexOf('\n'));
											TaskScheduler.ScheduleScript(GameManager, luablob, 8, null);

											sw.Write("");
											break;
										}
									default:
										sw.Write("");
										break;
								}
							}
						}
						catch
						{
							LogManager.LogError("Invalid data got in Remote Control Pipe, server might be hijacked");
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
