using MoonSharp.Interpreter;
using NetBlox.Common;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NetBlox.Instances.Services
{
	[Service]
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
				throw new ScriptRuntimeException("Cannot start remote control pipe in client");

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
		[Lua([Security.Capability.CoreSecurity])]
		public ByteArray SignString(string text, ByteArray pk, ByteArray sk)
		{
			using SHA256 alg = SHA256.Create();
			using RSA rsa = RSA.Create();

			byte[] text8 = Encoding.Unicode.GetBytes(text);
			byte[] pk8 = pk.Data;
			byte[] sk8 = sk.Data;

			rsa.ImportRSAPublicKey(pk8, out _);
			rsa.ImportRSAPrivateKey(sk8, out _);
			byte[] sign = rsa.SignData(text8, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

			return new ByteArray([.. BitConverter.GetBytes((ushort)sign.Length), ..sign, .. text8]);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public bool VerifySignature(string stext, ByteArray pk)
		{
			using SHA256 alg = SHA256.Create();
			using RSA rsa = RSA.Create();

			byte[] text8 = Encoding.Unicode.GetBytes(stext);
			byte[] pk8 = pk.Data;

			rsa.ImportRSAPublicKey(pk8, out _);
			byte[] sign = rsa.SignData(text8, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			ushort signsize = BitConverter.ToUInt16(text8[0..2]);

			return rsa.VerifyData(text8[2..signsize], text8[(2 + signsize)..], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string GetDataFromSignedData(ByteArray stext)
		{
			byte[] text8 = stext.Data;
			ushort signsize = BitConverter.ToUInt16(text8[0..2]);
			return Encoding.Unicode.GetString(text8[(2 + signsize)..]);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public DynValue CreatePublicAndPrivateKey()
		{
			using SHA256 alg = SHA256.Create();
			using RSA rsa = RSA.Create();

			ByteArray sk = new ByteArray(rsa.ExportRSAPrivateKey());
			ByteArray pk = new ByteArray(rsa.ExportRSAPublicKey());

			var ser = SerializationManager.LuaSerializers["NetBlox.Structs.ByteArray"];

			return DynValue.NewTuple(ser(sk, GameManager), ser(pk, GameManager));
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PlatformService) == classname) return true;
			return base.IsA(classname);
		}
	}
}
