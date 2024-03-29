using Raylib_cs;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using NetBlox.Tools;
using NetBlox.Instances;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace NetBlox
{
    public static class AppManager
	{
		public static Dictionary<string, bool> FastFlags = new();
		public static Dictionary<string, string> FastStrings = new();
		public static Dictionary<string, int> FastNumbers = new();
		public static bool IsServer = false;
		public const int VersionMajor = 1;
		public const int VersionMinor = 1;
		public const int VersionPatch = 0;

		public static void Start(bool client, string name, string[] args)
		{
			ulong pid = ulong.MaxValue;
			LogManager.LogInfo("Initializing NetBlox...");

			// common thingies
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--fast-flag":
						{
							var key = args[++i];
							var bo = int.Parse(args[++i]) == 1;

							FastFlags[key] = bo;
							LogManager.LogInfo($"Setting fast flag {key} to {bo}");
							break;
						}
					case "--fast-string":
						{
							var key = args[++i];
							var st = args[++i];

							FastStrings[key] = st;
							LogManager.LogInfo($"Setting fast stirng {key} to \"{st}\"");
							break;
						}
					case "--fast-number":
						{
							var key = args[++i];
							var nu = int.Parse(args[++i]);

							FastNumbers[key] = nu;
							LogManager.LogInfo($"Setting fast number {key} to {nu}");
							break;
						}
					case "--debug-console":
						{
							LogManager.LogWarn("Starting debug console... (the security level is " + ConsoleTool.SecurityLevel + ")");
							ConsoleTool.Run();
							break;
						}
					case "--place-id":
						{
							pid = ulong.Parse(args[++i]);
							break;
						}
				}
			}

			LogManager.LogInfo("Initializing PlayManager...");
			PlayManager.Initialize();

			LogManager.LogInfo("Initializing RenderManager...");
			RenderManager.Initialize();

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

			TeleportToPlace(pid);
			GameManager.StartProcessing();
		}
		public static void TeleportToPlace(ulong pid)
		{
			PlayManager.ShowTeleportGui();

			LogManager.LogInfo($"Teleporting to the place ({pid})...");
			// no actual servers as of now, so just hardcoded values
			string pname = "Testing Place";
			ulong pauth = 1;
			Thread.Sleep(4000); // make it look like im doing smth
			LogManager.LogInfo($"Place has name ({pname}) and author ({pauth})...");
			GameManager.TeleportToServer(null!);

			PlayManager.HideTeleportGui();
		}
	}
}
