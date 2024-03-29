global using Color = Raylib_cs.Color;
using Raylib_cs;
using NetBlox.GUI;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace NetBlox
{
    public static class GameManager
	{
		public static List<Instance> AllInstances = new();
		public static Instance CurrentRoot = null!;
		public static ServerIdentity? CurrentIdentity;
		public static TcpClient? CurrentNetworkClient;
		public static bool IsRunning = false;
		public static int PreferredFPS = 60;
		public static Vector3 Acceleration = new Vector3(0, 0.001f, 0);
		public static string? UserName = "DevDevDev" + Random.Shared.Next(1000, 9999);
		public const ushort GamePort = 2556;
		public static event EventHandler? Shutdown;
		internal static bool ShuttingDown = false;
		internal static Queue<Message> MessageQueue = new();
		internal static GameplayPhase CurrentGameplayPhase = GameplayPhase.Black;

		public static void TeleportToServer(IPAddress? ipa)
		{
			if (AppManager.IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			LogManager.LogInfo($"Teleporting into server: {ipa}...");
			CurrentIdentity = null;
			CurrentGameplayPhase = GameplayPhase.Loading;

			// if (CurrentRoot != null)
			// {
			// 	CurrentRoot.Destroy();
			// }

			// we wont switch DataModel as of now, because we may or may not lose connection to the server during the process

			LuaRuntime.RunScript(string.Empty, false, null, 0, false); // we will run nothing to initialize lua

			Task.Run(() => // thats not a connection to server but who cares
			{
				DataModel dm = new();
				Workspace ws = new();
				ReplicatedStorage rs = new();
				ReplicatedFirst ri = new();
				RunService ru = new();
				Players pl = new();
				Camera cm = new();

				ws.MainCamera = cm;

				ws.Parent = dm;
				dm.Name = "Baseplate";

				Part part = new Part();

				part.Parent = ws;
				part.Color = Color.DarkGreen;
				part.Position = new(0, -5, 0);
				part.Size = new(50, 2, 20);
				part.TopSurface = SurfaceType.Studs;
				part.Anchored = true;

				cm.Parent = part.Parent;
				rs.Parent = dm;
				ri.Parent = dm;
				pl.Parent = dm;
				ru.Parent = dm;

				// i think we connected altough we didn't

				if (CurrentRoot != null)
					CurrentRoot.Destroy();
				CurrentRoot = dm;

				var player = CreateNewPlayer(pl, "DevDevDev", true);
				pl.LocalPlayer = player;
				player.LoadCharacter();

				IsRunning = true;
			});
		}
		public static Player CreateNewPlayer(Players pl, string name, bool local)
		{
			Player player = new Player();

			player.Name = name;
			player.Parent = pl;
			player.IsLocalPlayer = local;

			return player;
		}
		public static void StartProcessing()
		{
			try
			{
				var time = 0L;
				var running = true;

				LogManager.LogInfo("Starting game processing...");

				while (running)
				{
					try
					{
						if (MessageQueue.Count > 0)
						{
							var msg = MessageQueue.Dequeue();

							switch (msg.Type)
							{
								case MessageType.Timer:
									time++;
									if (CurrentRoot != null && IsRunning)
										ProcessInstance(CurrentRoot);
									break;
								case MessageType.Shutdown:
									if (Shutdown != null)
										Shutdown(new(), new());
									ShuttingDown = true;
									running = false;
									break;
								default:
									break;
							}
						}
					}
					catch (Exception ex)
					{
						if (CurrentRoot != null)
						{
							CurrentRoot.Destroy();
							CurrentRoot = null!;
						}

						RenderManager.ScreenGUI.Add(new GUI.GUI()
						{
							CorrespondingPhase = CurrentGameplayPhase,
							Elements = {
								new GUIFrame(new UDim2(0.25f, 0.175f), new UDim2(0.5f, 0.5f), Color.Red),
								new GUIText("Engine internal error: " + ex.GetType().Name + ", " + ex.Message + ".\nPlease consider restarting NetBlox", new UDim2(0.5f, 0.5f))
							}
						});
					}
				}
			}
			catch
			{
				LogManager.LogError("Game processor had failed!");
				Environment.Exit(1);
			}
		}
		public static void SetPreferredFPS(int fps)
		{
			PreferredFPS = fps;
			Raylib.SetTargetFPS(fps);
		}
		public static Instance? GetInstance(Guid id)
		{
			for (int i = 0; i < AllInstances.Count; i++)
			{
				if (AllInstances[i].UniqueID == id)
					return AllInstances[i];
			}
			return null;
		}
		public static void ProcessInstance(Instance inst)
		{
			inst.Process();
			var ch = inst.GetChildren();
			for (int i = 0; i < ch.Length; i++)
			{
				ProcessInstance(ch[i]);
			}
		}
		public static T? GetService<T>() where T : Instance
		{
			foreach (var inst in CurrentRoot.Children)
				if (inst is T) 
					return (T)inst;
			return null;
		}
	}
	public struct Message
	{
		public MessageType Type;
		public NetworkPacket? Packet;
		public string? Text;
		public long Number;
		public float Float;
	}
	public enum GameplayPhase
	{
		Gameplay, Loading, Disconnect, Black
	}
	public enum MessageType
	{
		Timer, Replicate, Reparent, PropertyChange, Shutdown
	}
}