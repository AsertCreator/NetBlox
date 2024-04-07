using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using Network.Enums;
using Network.Extensions;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetBlox
{
	public static class NetworkManager
	{
		private struct ClientHandshake
		{
			public string? Username;
			public int VersionMajor;
			public int VersionMinor;
			public int VersionPatch;
		}
		private struct ServerHandshake
		{
			public string PlaceName;
			public string UniverseName;
			public string Author;

			public ulong PlaceID;
			public ulong UniverseID;
			public uint MaxPlayerCount;
			public uint UniquePlayerID;

			public int ErrorCode;
			public string ErrorMessage;

			public int InstanceCount;
			public Guid DataModelInstance;
			public Guid PlayerInstance;
			public Guid CharacterInstance;
		}
		public class Replication
		{
			public List<NetworkClient> To = GameManager.AllClients;
			public Instance? What;
		}
		private static readonly JsonSerializerOptions DefaultJSON = new()
		{
			IncludeFields = true
		};
		public static TcpClient NetworkClient = null!;
		public static TcpListener NetworkServer = null!;
		public static bool IsServer { get; private set; }
		public static bool IsClient { get; private set; }
		public static int MainPort { get; private set; } = 2556;
		public static Queue<Replication> ToReplicate = new();
		public static Connection? ServerConnection;
		private static uint NextPID = 0;
		private static bool init;

		public static void Initialize(bool server, bool client)
		{
			if (!init)
			{
				IsServer = server;
				IsClient = client;
				init = true;
			}
		}
		public static void Disconnect(NetworkClient nc)
		{
			if (!IsServer)
				throw new NotSupportedException("Attempt to disconnect another player from client");

			nc.IsDisconnecting = true;
			if (nc.Player != null)
			{
				if (nc.Player.Character != null)
					nc.Player.Character.Destroy();
				nc.Player.Destroy();
			}
			Console.WriteLine($"{nc.Username} had disconnected!");
			GameManager.AllClients.Remove(nc);
		}
		public static void StartServer()
		{
			if (!IsServer)
				throw new NotSupportedException("Cannot start server in non-server configuration!");

			LogManager.LogInfo($"Starting listening for server connections at {MainPort}...");
			ServerConnectionContainer scc = ConnectionFactory.CreateServerConnectionContainer(MainPort);
			scc.ConnectionEstablished += (_x, _y) =>
			{
				LogManager.LogInfo($"Connection established with {_x.IPRemoteEndPoint.Address}!");
				_x.RegisterRawDataHandler("nb.init-rep-req", (x, y) =>
				{
					SeqReplicateInstance(y, GameManager.CurrentRoot, true);
				});
				_x.RegisterRawDataHandler("nb.handshake", (x, y) =>
				{
					ServerHandshake sh = new ServerHandshake();
					ClientHandshake ch = DeserializeJsonBytes<ClientHandshake>(x.Data);
					NetworkClient nc = new NetworkClient();
					Players pls = GameManager.GetService<Players>()!;
					Player plr = new Player();

					nc.Username = ch.Username;
					nc.Connection = y;
					nc.UniquePlayerID = NextPID++;
					nc.IsDisconnecting = false;
					nc.Player = plr;

					GameManager.AllClients.Add(nc);

					plr.Name = nc.Username ?? string.Empty;
					plr.Parent = pls;
					plr.LoadCharacter();

					sh.PlayerInstance = plr.UniqueID;
					sh.CharacterInstance = plr.Character.UniqueID;
					sh.DataModelInstance = GameManager.CurrentRoot.UniqueID;
					sh.InstanceCount = GameManager.AllInstances.Count;

					y.SendRawData("nb.placeinfo", SerializeJsonBytes(sh));

					LogManager.LogInfo($"Successfully performed handshake with {ch.Username}!");

					_x.ConnectionClosed += (f, g) =>
					{
						Disconnect(nc);
					};
					_x.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
					{
						var ins = SeqReceiveInstance(_x, x.ToUTF8String());
						var to = GameManager.AllClients;
						to.Remove(nc);
						if (to.Count != 0)
							ToReplicate.Enqueue(new Replication()
							{
								To = to,
								What = ins
							});
					});
				});
			};
			Task.Run(() =>
			{
				while (!GameManager.ShuttingDown)
				{
					try
					{
						while (ToReplicate.Count == 0) ;
						var tr = ToReplicate.Dequeue();
						var ins = tr.What;
						var to = tr.To;

						for (int i = 0; i < to.Count; i++) 
							SeqReplicateInstance(to[i].Connection!, ins!, false);
					}
					catch
					{
						LogManager.LogError($"Could not replicate queued instance, well i dont care!");
					}
				}
			});
		}
		public static void ConnectToServer(IPAddress ipa)
		{
			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			Task.Run(() =>
			{
				RenderManager.Status = string.Empty;
				RenderManager.ShowTeleportGui();

				try
				{
					LogManager.LogInfo($"Teleporting into server: {ipa}...");
					GameManager.CurrentIdentity.Reset();

					GameManager.IsRunning = false;

					LuaRuntime.Threads.Clear();
					GameManager.AllInstances.Clear();

					try
					{
						LogManager.LogInfo("Starting client network thread...");

						var res = ConnectionResult.TCPConnectionNotAlive;
						var con = ConnectionFactory.CreateTcpConnection(ipa.ToString(), MainPort, out res);

						ServerConnection = con;
						con.EnableLogging = true;

						if (res == ConnectionResult.Connected)
						{
							LogManager.LogInfo($"Connected to {ipa}, performing C>S handshake...");
							con.ConnectionClosed += (x, y) => 
							{ 
								LogManager.LogInfo($"Disconnected from server due to " + x);
								(GameManager.GetService<Players>().LocalPlayer as Player).Kick($"Disconnected from server due to " + x);
							};

							ServerHandshake sh = default;
							ClientHandshake ch = new();
							ch.Username = GameManager.Username;
							ch.VersionMajor = GameManager.VersionMajor;
							ch.VersionMinor = GameManager.VersionMinor;
							ch.VersionPatch = GameManager.VersionPatch;

							Thread.Sleep(50); // wait for server to do things

							con.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								if (ins.UniqueID == sh.DataModelInstance)
								{
									GameManager.CurrentRoot = (DataModel)ins;
									GameManager.IsRunning = true;
								}
								if (ins.UniqueID == sh.PlayerInstance)
								{
									var plr = (Player)ins;
									var pls = GameManager.GetService<Players>();
									plr.IsLocalPlayer = true;
									pls.LocalPlayer = plr;
								}
								if (ins.UniqueID == sh.CharacterInstance)
								{
									var chr = (Character)ins;
									chr.IsLocalPlayer = true;
									var cam = new Camera();
									cam.Parent = GameManager.GetService<Workspace>();
									cam.CameraSubject = chr;
								}
							});
							con.RegisterRawDataHandler("nb.placeinfo", (x, y) =>
							{
								sh = DeserializeJsonBytes<ServerHandshake>(x.Data);

								if (sh.ErrorCode != 0)
								{
									var msg = $"Could not connect to the server! {sh.ErrorCode} - {sh.ErrorMessage}";
									LogManager.LogError(msg);
									RenderManager.Status = msg;
									return;
								}

								GameManager.CurrentIdentity.PlaceName = sh.PlaceName;
								GameManager.CurrentIdentity.PlaceID = sh.PlaceID;
								GameManager.CurrentIdentity.UniverseName = sh.UniverseName;
								GameManager.CurrentIdentity.UniverseID = sh.UniverseID;
								GameManager.CurrentIdentity.Author = sh.Author;
								GameManager.CurrentIdentity.UniquePlayerID = sh.UniquePlayerID;
								GameManager.CurrentIdentity.MaxPlayerCount = sh.MaxPlayerCount;

								con.SendRawData("nb.init-rep-req", []); // we request server to download game into us

								RenderManager.HideTeleportGui();
							});

							con.SendRawData("nb.handshake", SerializeJsonBytes(ch));
						}
					}
					catch (Exception ex)
					{
						var msg = $"Could not connect to the server! {ex.GetType().Name} - {ex.Message}";
						var pls = GameManager.GetService<Players>();
						if (pls == null) return;
						var plr = pls.LocalPlayer as Player;

						LogManager.LogError(msg);

						if (plr != null)
							plr.Kick(msg);

						return;
					}
				}
				catch (Exception ex)
				{
					RenderManager.Status = $"Could not connect due to error! {ex.GetType().FullName}: {ex.Message}";
					LogManager.LogError(RenderManager.Status);
				}
			});
		}
		public static byte[] SerializeJsonBytes<T>(T obj) => Encoding.UTF8.GetBytes(SerializeJson(obj));
		public static string SerializeJson<T>(T obj) => JsonSerializer.Serialize(obj, DefaultJSON);
		public static T? DeserializeJsonBytes<T>(byte[] d) => DeserializeJson<T>(Encoding.UTF8.GetString(d));
		public static T? DeserializeJson<T>(string d) => JsonSerializer.Deserialize<T>(d, DefaultJSON);
		public static void SeqReplicateInstance(Connection c, Instance ins, bool repchildren)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				var typ = ins.GetType();
				var prs = typ.GetProperties();

				for (int i = 0; i < prs.Length; i++)
				{
					var prop = prs[i];
					var val = prop.GetValue(ins);
					if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
					if (val == null) continue;

					dss[prop.Name] = SerializationManager.Serialize(val);
				}

				c.SendRawData("nb.inc-inst", SerializeJsonBytes(dss));

				Thread.Sleep(5); // give it some rest c'mon

				if (repchildren)
				{
					var ch = ins.GetChildren();
					for (int i = 0; i < ch.Length; i++)
						SeqReplicateInstance(c, ch[i], true);
				}
			}
			catch
			{
				// we've failed a little bit
			}
		}
		public static Instance SeqReceiveInstance(Connection c, string tag)
		{
			var inf = JsonSerializer.Deserialize<Dictionary<string, string>>(tag);
			var uid = Guid.Parse(inf["UniqueID"]);
			var pre = (from x in GameManager.AllInstances where x.UniqueID == uid select x).FirstOrDefault();
			if (pre == null)
			{
				var ins = InstanceCreator.CreateInstance(inf["ClassName"]);
				var typ = ins.GetType();
				var prs = typ.GetProperties();

				ins.WasReplicated = true;

				for (int i = 0; i < inf.Count; i++)
				{
					try
					{
						var kvp = inf.ElementAt(i);
						var prop = Array.Find(prs, x => x.Name == kvp.Key);
						if (prop == null) continue;
						if (prop.CanWrite)
							prop.SetValue(ins, SerializationManager.Deserialize(prop.PropertyType, inf[kvp.Key]));
					}
					catch
					{
						// we had failed
					}
				}

				ins.Parent = (from x in GameManager.AllInstances where x.UniqueID == ins.ParentID select x).FirstOrDefault();

				return ins;
			}
			else
			{
				var typ = pre.GetType();
				var prs = typ.GetProperties();

				for (int i = 0; i < inf.Count; i++)
				{
					try
					{
						var kvp = inf.ElementAt(i);
						var prop = Array.Find(prs, x => x.Name == kvp.Key);
						if (prop == null) continue;
						if (prop.CanWrite)
							prop.SetValue(pre, SerializationManager.Deserialize(prop.PropertyType, inf[kvp.Key]));
					}
					catch
					{
						// we had failed
					}
				}

				return pre;
			}
		}
	}
}
