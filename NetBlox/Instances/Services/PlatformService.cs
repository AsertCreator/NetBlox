using MoonSharp.Interpreter;
using NetBlox.Runtime;
using Raylib_cs;
using System.IO.Pipes;

namespace NetBlox.Instances.Services
{
	public class PlatformService : Instance
	{
		public static HttpClient HttpClient = new();
		public static Action<string> QueuedTeleport = (xo) => { throw new Exception("NetBlox died!"); };
		[Lua([Security.Capability.CoreSecurity])]
		public bool IsStudio => GameManager.IsStudio;

		public PlatformService(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.CoreSecurity])]
		public void BeginQueuedTeleport() => QueuedTeleport(GameManager.QueuedTeleportAddress);
		[Lua([Security.Capability.CoreSecurity])]
		public string[] GetConsoleArguments() => Environment.GetCommandLineArgs();
		[Lua([Security.Capability.CoreSecurity])]
		public string HttpGet(string url)
		{
			var task = HttpClient.GetAsync(url);
			task.Wait();
			var task2 = task.Result.Content.ReadAsStringAsync();
			task2.Wait();
			return task2.Result;
		}
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
		public void OpenBrowser(string url)
		{
			Raylib.OpenURL(url);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void SetPreference(string key, string val)
		{
			AppManager.SetPreference(key, val);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string GetPreference(string key)
		{
			return AppManager.GetPreference(key);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void ConnectToServer(string addr)
		{
			GameManager.NetworkManager.ConnectToServer(System.Net.IPAddress.Parse(addr));
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Disconnect()
		{
			GameManager.NetworkManager.DisconnectFromServer(Network.Enums.CloseReason.ClientClosed);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string FormatVersion() => $"NetBlox {(IsStudio ? "Studio" : "Client")}, v{AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}";
		[Lua([Security.Capability.CoreSecurity])]
		public void EnableStatusPipe()
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new Exception("Cannot start status pipe in client");

			System.Diagnostics.Process pr = System.Diagnostics.Process.GetCurrentProcess();

			Task.Run(() =>
			{
				while (true)
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
							sw.WriteLine(GameManager.AllClients.Count);
							sw.WriteLine(GameManager.CurrentIdentity.MaxPlayerCount);

							sw.Flush();
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
