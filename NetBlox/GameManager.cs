global using Color = Raylib_cs.Color;
using NetBlox.GUI;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;
using System.Net.Sockets;
using System.Runtime;

namespace NetBlox
{
	public static class GameManager
	{
		public static Dictionary<string, bool> FastFlags = new();
		public static Dictionary<string, string> FastStrings = new();
		public static Dictionary<string, int> FastNumbers = new();
		public static Dictionary<char, Action> Verbs = new();
		public static List<Instance> AllInstances = new();
		public static ServerIdentity? CurrentIdentity;
		public static TcpClient? CurrentNetworkClient;
		public static DataModel CurrentRoot = null!;
		public static bool IsServer = false;
		public static bool IsRunning = false;
		public static string ContentFolder = "content/";
		public static string? UserName = "DevDevDev" + Random.Shared.Next(1000, 9999);
		public static event EventHandler? Shutdown;
		public const ushort GamePort = 2556;
		public const int VersionMajor = 1;
		public const int VersionMinor = 2;
		public const int VersionPatch = 0;
		public static Queue<Message> MessageQueue = new();
		public static bool ShuttingDown = false;

		public static void Start(bool client, string[] args)
		{
			IsServer = !client;
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
					case "--place-id":
						{
							pid = ulong.Parse(args[++i]);
							break;
						}
				}
			}

			LogManager.LogInfo("Initializing RenderManager...");
			RenderManager.Initialize();

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

			TeleportToPlace(pid);
			StartProcessing();
		}
		public static void TeleportToPlace(ulong pid)
		{
			Task.Run(() =>
			{
				LogManager.LogInfo($"Teleporting to the place ({pid})...");
				// no actual servers as of now, so just hardcoded values
				string pname = "Testing Place";
				ulong pauth = 1;
				LogManager.LogInfo($"Place has name ({pname}) and author ({pauth})...");
				TeleportToServer(null!);
			});
		}
		public static void TeleportToServer(IPAddress? ipa)
		{
			RenderManager.ShowTeleportGui();

			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			LogManager.LogInfo($"Teleporting into server: {ipa}...");
			CurrentIdentity = null;

			IsRunning = false;
			LuaRuntime.Threads.Clear();

			// if (CurrentRoot != null)
			// {
			// 	CurrentRoot.Destroy();
			// }

			// we wont switch DataModel as of now, because we may or may not lose connection to the server during the process

			Task.Run(() => // thats not a connection to server but who cares
			{
				DataModel dm = new();
				Workspace ws = new();
				ReplicatedStorage rs = new();
				ReplicatedFirst ri = new();
				RunService ru = new();
				Players pl = new();
				Camera cm = new();

				Thread.Sleep(4000); // make it look like im doing smth

				ws.MainCamera = cm;

				ws.Parent = dm;
				dm.Name = "Baseplate";

				Part part = new()
				{
					Parent = ws,
					Color = Color.DarkGreen,
					Position = new(0, -5, 0),
					Size = new(50, 2, 20),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};

				cm.Parent = part.Parent;
				rs.Parent = dm;
				ri.Parent = dm;
				pl.Parent = dm;
				ru.Parent = dm;

				// i think we connected altough we didn't

				CurrentRoot?.Destroy();
				CurrentRoot = dm;

				var player = pl.CreateNewPlayer("DevDevDev", true);
				pl.LocalPlayer = player;
				player.LoadCharacter();

				LuaRuntime.Setup(CurrentRoot);
				LuaRuntime.Execute(string.Empty, 0, null, CurrentRoot); // we will run nothing to initialize lua

				IsRunning = true;

				RenderManager.HideTeleportGui();
			});
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
									{
										ProcessInstance(CurrentRoot);
										if (LuaRuntime.CurrentThread != null)
										{
											if (LuaRuntime.CurrentThread.Value.Coroutine == null)
												LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
											else if (DateTime.Now >= LuaRuntime.CurrentThread.Value.WaitUntil &&
												LuaRuntime.CurrentThread.Value.Coroutine.State != MoonSharp.Interpreter.CoroutineState.Dead)
											{
												var cst = new CancellationTokenSource();
												var tsk = Task.Run(() =>
												{
#pragma warning disable SYSLIB0046 // Type or member is obsolete
													ControlledExecution.Run(() =>
													{
														var res = LuaRuntime.CurrentThread.Value.Coroutine.Resume();
														if (res.Type == MoonSharp.Interpreter.DataType.YieldRequest)
															return;
														else
														{
															LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
														}
													}, cst.Token);
#pragma warning restore SYSLIB0046 // Type or member is obsolete
												});
												if (!tsk.Wait(LuaRuntime.ScriptExecutionTimeout * 1000))
												{
													LuaRuntime.PrintError("Exhausted maximum script execution time!");
													cst.Cancel();
												}
											}
											else
												LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);

											if (LuaRuntime.Threads.Count > 0)
												LuaRuntime.CurrentThread = LuaRuntime.CurrentThread.Next;
											else
												LuaRuntime.CurrentThread = null;
										}
										else
										{
											LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
										}
									}
									break;
								case MessageType.Shutdown:
									Shutdown?.Invoke(new(), new());
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
							Elements = {
								new GUIFrame(new UDim2(0.25f, 0.175f), new UDim2(0.5f, 0.5f), Color.Red),
								new GUIText("Engine public error: " + ex.GetType().Name + ", " + ex.Message + ".\nPlease consider restarting NetBlox", new UDim2(0.5f, 0.5f))
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
				if (inst is T t)
					return t;
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
	public enum MessageType
	{
		Timer, Replicate, Reparent, PropertyChange, Shutdown
	}
}