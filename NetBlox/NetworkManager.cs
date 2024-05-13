using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using Network.Enums;
using Network.Extensions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBlox
{
	public class NetworkManager
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
			public List<NetworkClient> To;
			public Instance? What;
			public bool RepChildren = true;
			public bool AsService = false;
			public Action? Callback;
		}
		private static readonly JsonSerializerOptions DefaultJSON = new()
		{
			IncludeFields = true
		};
		public TcpClient NetworkClient = null!;
		public TcpListener NetworkServer = null!;
		public GameManager GameManager;
		public bool IsServer { get; private set; }
		public bool IsClient { get; private set; }
		public int ServerPort { get; private set; } = 2556;
		public int ClientPort { get; private set; } = 6552;
		public Queue<Replication> ToReplicate = new();
		public Connection? ServerConnection;
		private uint NextPID = 0;
		private bool init;
		private int loaded = 0;

		public NetworkManager(GameManager gm, bool server, bool client)
		{
			GameManager = gm;
			if (!init)
			{
				IsServer = server;
				IsClient = client;
				init = true;
			}
		}

		public void Disconnect(NetworkClient nc)
		{
			if (!IsServer)
				throw new NotSupportedException("Attempt to disconnect another player from client");

			nc.IsDisconnecting = true;
			if (nc.Player != null)
			{
				nc.Player.Character?.Destroy();
				nc.Player.Destroy();
			}
			LogManager.LogInfo($"{nc.Username} had disconnected!");
			GameManager.AllClients.Remove(nc);
		}
		public void DisconnectFromServer(CloseReason cr)
		{
			if (NetworkClient != null)
			{
				NetworkClient.Close();
				LogManager.LogInfo("Disconnected from server due to " + cr + "!");
			}

			GameManager.CurrentRoot.GetService<ReplicatedFirst>().Destroy();
			GameManager.CurrentRoot.GetService<Players>().Destroy();
			GameManager.CurrentRoot.GetService<Workspace>().Destroy();
			GameManager.CurrentRoot.GetService<ReplicatedStorage>().Destroy();
		}
		public void StartServer()
		{
			if (!IsServer)
				throw new NotSupportedException("Cannot start server in non-server configuration!");

			LogManager.LogInfo($"Starting listening for server connections at {ServerPort}...");
			ServerConnectionContainer scc = ConnectionFactory.CreateServerConnectionContainer(ServerPort);
			scc.ConnectionEstablished += (_x, _y) =>
			{
				LogManager.LogInfo($"Connection established with {_x.IPRemoteEndPoint.Address}!");
				_x.RegisterRawDataHandler("nb.handshake", (x, y) =>
				{
					ServerHandshake sh = new ServerHandshake();
					ClientHandshake ch = DeserializeJsonBytes<ClientHandshake>(x.Data);
					NetworkClient nc = new NetworkClient();
					Players pls = GameManager.CurrentRoot.GetService<Players>()!;
					Backpack bck = new Backpack(GameManager);
					PlayerGui pg = new PlayerGui(GameManager);
					Player plr = new Player(GameManager);

					nc.Username = ch.Username;
					nc.Connection = y;
					nc.UniquePlayerID = NextPID++;
					nc.IsDisconnecting = false;
					nc.Player = plr;

					pg.Reload();
					bck.Reload();

					bck.Parent = plr;
					pg.Parent = plr;

					GameManager.AllClients.Add(nc);

					plr.Name = nc.Username ?? string.Empty;
					plr.Parent = pls;
					plr.LoadCharacter();

					sh.PlaceName = GameManager.CurrentIdentity.PlaceName;
					sh.UniverseName = GameManager.CurrentIdentity.UniverseName;
					sh.Author = GameManager.CurrentIdentity.Author;
					sh.PlaceID = GameManager.CurrentIdentity.PlaceID;
					sh.UniverseID = GameManager.CurrentIdentity.UniverseID;
					sh.UniquePlayerID = nc.UniquePlayerID;
					sh.PlayerInstance = plr.UniqueID;
					sh.CharacterInstance = plr.Character.UniqueID;
					sh.DataModelInstance = GameManager.CurrentRoot.UniqueID;
					sh.InstanceCount = 
						GameManager.CurrentRoot.GetService<ReplicatedFirst>().CountDescendants() +
						GameManager.CurrentRoot.GetService<Players>().CountDescendants() +
						GameManager.CurrentRoot.GetService<Workspace>().CountDescendants() +
						GameManager.CurrentRoot.GetService<ReplicatedStorage>().CountDescendants();

					y.SendRawData("nb.placeinfo", SerializeJsonBytes(sh));

					LogManager.LogInfo($"Successfully performed handshake with {ch.Username}!");

					Thread.Sleep(4000);

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
						{
							lock (ToReplicate)
							{
								ToReplicate.Enqueue(new Replication()
								{
									To = to,
									What = ins
								});
							}
						}
					});
					_x.RegisterRawDataHandler("nb.req-int-rep", (x, y) =>
					{
						lock (ToReplicate)
						{
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot,
								RepChildren = false
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<ReplicatedFirst>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<Players>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<Workspace>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = GameManager.CurrentRoot.GetService<ReplicatedStorage>(),
								AsService = true
							});
						}
					});

					Task.Run(() =>
					{
						while (!GameManager.ShuttingDown)
						{
							try
							{
								while (ToReplicate.Count == 0) ;
								lock (ToReplicate)
								{
									var tr = ToReplicate.Dequeue();
									var ins = tr.What;
									var to = tr.To;

									for (int i = 0; i < to.Count; i++)
										SeqReplicateInstance(to[i].Connection!, ins!, tr.RepChildren, tr.AsService);

									if (tr.Callback != null)
										tr.Callback();
								}
							}
							catch
							{
								LogManager.LogError($"Could not replicate queued instance, well i dont care!");
							}
						}
					});

					GameManager.AllowReplication = true;
				});
			};
		}
		public void ConnectToServer(IPAddress ipa)
		{
			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			Task.Run(() =>
			{
				GameManager.RenderManager.Status = string.Empty;

				try
				{
					LogManager.LogInfo($"Teleporting into server: {ipa}...");
					GameManager.CurrentIdentity.Reset();
					LuaRuntime.Threads.Clear();

					try
					{
						LogManager.LogInfo("Starting client network thread...");

						var res = ConnectionResult.TCPConnectionNotAlive;
						var con = ConnectionFactory.CreateTcpConnection(ipa.ToString(), ServerPort, out res);

						ServerConnection = con;
						con.EnableLogging = false;

						if (res == ConnectionResult.Connected)
						{
							LogManager.LogInfo($"Connected to {ipa}, performing C>S handshake...");
							con.ConnectionClosed += (x, y) => 
							{
								DisconnectFromServer(x);
							};

							ServerHandshake sh = default;
							ClientHandshake ch = new();
							ch.Username = GameManager.Username;
							ch.VersionMajor = SharedData.VersionMajor;
							ch.VersionMinor = SharedData.VersionMinor;
							ch.VersionPatch = SharedData.VersionPatch;

							con.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								if (ins.UniqueID == sh.DataModelInstance)
								{
									GameManager.CurrentRoot.Name = (ins as DataModel)!.Name;
									GameManager.IsRunning = true;

									con.RegisterRawDataHandler("nb.repar-inst", (x, y) =>
									{
										var dss = DeserializeJsonBytes<Dictionary<string, string>>(x.Data);
										var ins = GameManager.GetInstance(Guid.Parse(dss["Instance"]));
										var par = GameManager.GetInstance(Guid.Parse(dss["Parent"]));

										if (ins == null || par == null)
										{
											LogManager.LogError("Failed to reparent instance, because new parent does not exist");
											return;
										}
										else
										{
											ins.Parent = par;
										}
									});
								}
								if (ins.UniqueID == sh.PlayerInstance)
								{
									var plr = (Player)ins;
									var pls = GameManager.CurrentRoot.GetService<Players>();
									plr.IsLocalPlayer = true;
									pls.LocalPlayer = plr;
								}
								if (ins.UniqueID == sh.CharacterInstance)
								{
									var chr = (Character)ins;
									chr.IsLocalPlayer = true;
									var cam = new Camera(GameManager);
									cam.Parent = GameManager.CurrentRoot.GetService<Workspace>();
									cam.CameraSubject = chr;
								}
								if (loaded++ == sh.InstanceCount)
								{
									GameManager.CurrentRoot.GetService<CoreGui>().HideTeleportGui();
								}
							});
							con.RegisterRawDataHandler("nb.inc-service", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								ins.Parent = GameManager.CurrentRoot;
							});
							con.RegisterRawDataHandler("nb.placeinfo", (x, y) =>
							{
								sh = DeserializeJsonBytes<ServerHandshake>(x.Data);

								if (sh.ErrorCode != 0)
								{
									var msg = $"Could not connect to the server! {sh.ErrorCode} - {sh.ErrorMessage}";
									LogManager.LogError(msg);
									GameManager.RenderManager.Status = msg;
									return;
								}

								GameManager.CurrentIdentity.PlaceName = sh.PlaceName;
								GameManager.CurrentIdentity.PlaceID = sh.PlaceID;
								GameManager.CurrentIdentity.UniverseName = sh.UniverseName;
								GameManager.CurrentIdentity.UniverseID = sh.UniverseID;
								GameManager.CurrentIdentity.Author = sh.Author;
								GameManager.CurrentIdentity.UniquePlayerID = sh.UniquePlayerID;
								GameManager.CurrentIdentity.MaxPlayerCount = sh.MaxPlayerCount;

								GameManager.CurrentRoot.GetService<CoreGui>().ShowTeleportGui(sh.PlaceName, sh.Author, (int)sh.PlaceID, 0);

								y.SendRawData("nb.req-int-rep", []);
							});

							con.SendRawData("nb.handshake", SerializeJsonBytes(ch));
						}
					}
					catch (Exception ex)
					{
						var msg = $"Could not connect to the server! {ex.GetType().Name} - {ex.Message}";
						var pls = GameManager.CurrentRoot.GetService<Players>();
						if (pls == null) return;
						var plr = pls.LocalPlayer as Player;

						LogManager.LogError(msg);

						plr?.Kick(msg);

						return;
					}
				}
				catch (Exception ex)
				{
					GameManager.RenderManager.Status = $"Could not connect due to error! {ex.GetType().FullName}: {ex.Message}";
					LogManager.LogError(GameManager.RenderManager.Status);
				}
			});
		}
		public byte[] SerializeJsonBytes<T>(T obj) => Encoding.UTF8.GetBytes(SerializeJson(obj));
		public string SerializeJson<T>(T obj) => JsonSerializer.Serialize(obj, DefaultJSON);
		public T? DeserializeJsonBytes<T>(byte[] d) => DeserializeJson<T>(Encoding.UTF8.GetString(d));
		public T? DeserializeJson<T>(string d) => JsonSerializer.Deserialize<T>(d, DefaultJSON);
		public void SeqReparentInstance(Connection c, Instance ins)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				dss["Instance"] = ins.UniqueID.ToString();
				dss["Parent"] = ins.ParentID.ToString();
				c.SendRawData("nb.repar-inst", SerializeJsonBytes(dss));
			}
			catch
			{
				LogManager.LogError($"Failed to perform network instance reparent ({ins.UniqueID}->{ins.ParentID})!");
			}
		}
		public void SeqReplicateInstance(Connection c, Instance ins, bool repchildren, bool asservice)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				var typ = ins.GetType();
				var prs = typ.GetProperties();

				if (typ.GetCustomAttribute<NotReplicatedAttribute>() != null) // we dont do THAT
					return;

				for (int i = 0; i < prs.Length; i++)
				{
					var prop = prs[i];
					var val = prop.GetValue(ins);
					if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null) continue;
					if (val == null) continue;

					dss[prop.Name] = SerializationManager.Serialize(val);
				}

				var key = "nb." + ins.UniqueID;

				c.RegisterRawDataHandler(key, (x, y) =>
				{
					y.UnRegisterRawDataHandler(key);
					if (repchildren)
					{
						var ch = ins.GetChildren();
						for (int i = 0; i < ch.Length; i++)
							SeqReplicateInstance(c, ch[i], true, false);
					}
				});

				if (!asservice)
					c.SendRawData("nb.inc-inst", SerializeJsonBytes(dss));
				else
					c.SendRawData("nb.inc-service", SerializeJsonBytes(dss));
			}
			catch
			{
				LogManager.LogError($"Failed to replicate instance ({ins.UniqueID})!");
			}
		}
		public Instance SeqReceiveInstance(Connection c, string tag)
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

				var key = "nb." + ins.UniqueID;

				c.SendRawData(key, []);

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
