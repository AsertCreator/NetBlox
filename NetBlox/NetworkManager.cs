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
	public sealed class NetworkManager
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
		public TcpClient NetworkClient = null!;
		public TcpListener NetworkServer = null!;
		public GameManager GameManager;
		public bool IsServer;
		public bool IsClient;
		public int ServerPort = 2556;
		public int ClientPort = 6552;
		public Queue<Replication> ToReplicate = new();
		public Connection? ServerConnection;
		private DataModel Root => GameManager.CurrentRoot;
		private uint NextPID = 0;
		private bool init;
		private bool filtermutex = false;
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
		public void DisconnectFromServer(Network.Enums.CloseReason cr)
		{
			if (NetworkClient != null)
			{
				NetworkClient.Close();
				LogManager.LogInfo("Disconnected from server due to " + cr + "!");
			}

			Root.GetService<ReplicatedFirst>().Destroy();
			Root.GetService<Players>().Destroy();
			Root.GetService<Workspace>().Destroy();
			Root.GetService<ReplicatedStorage>().Destroy();
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
					ClientHandshake ch = SerializationManager.DeserializeJsonBytes<ClientHandshake>(x.Data);
					NetworkClient nc = new NetworkClient();
					Players pls = Root.GetService<Players>()!;
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
					sh.DataModelInstance = Root.UniqueID;
					sh.InstanceCount = 
						Root.GetService<ReplicatedFirst>().CountDescendants() +
						Root.GetService<Players>().CountDescendants() +
						Root.GetService<Workspace>().CountDescendants() +
						Root.GetService<ReplicatedStorage>().CountDescendants();

					y.SendRawData("nb.placeinfo", SerializationManager.SerializeJsonBytes(sh));

					LogManager.LogInfo($"Successfully performed handshake with {ch.Username}!");

					Thread.Sleep(40);

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
								What = Root,
								RepChildren = false
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = Root.GetService<ReplicatedFirst>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = Root.GetService<Players>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = Root.GetService<Workspace>(),
								AsService = true
							});
							ToReplicate.Enqueue(new Replication()
							{
								To = [nc],
								What = Root.GetService<ReplicatedStorage>(),
								AsService = true
							});
						}
					});
					_x.RegisterRawDataHandler("nb.filter-string", (x, y) =>
					{
						var dss = SerializationManager.DeserializeJsonBytes<Dictionary<string, string>>(x.Data)!;
						var text = dss["Text"];

						y.SendRawData("nb.filter-string-out", Encoding.UTF8.GetBytes(GameManager.FilterString(text)));
					});

					GameManager.AllowReplication = true;
				});
			};
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
							var to = tr.To ?? GameManager.AllClients;

							for (int i = 0; i < to.Count; i++)
								SeqReplicateInstance(to[i].Connection!, ins!, tr.RepChildren, tr.AsService);

							if (tr.Callback != null)
								tr.Callback();
						}
					}
					catch (Exception ex)
					{
						LogManager.LogError($"Could not replicate queued instance, well i dont care!");
					}
				}
			});
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
							ch.VersionMajor = AppManager.VersionMajor;
							ch.VersionMinor = AppManager.VersionMinor;
							ch.VersionPatch = AppManager.VersionPatch;

							con.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								if (ins.UniqueID == sh.DataModelInstance)
								{
									Root.Name = (ins as DataModel)!.Name;
									GameManager.IsRunning = true;

									con.RegisterRawDataHandler("nb.repar-inst", (x, y) =>
									{
										var dss = SerializationManager.DeserializeJsonBytes<Dictionary<string, string>>(x.Data);
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
									var pls = Root.GetService<Players>();
									plr.IsLocalPlayer = true;
									pls.LocalPlayer = plr;
								}
								if (ins.UniqueID == sh.CharacterInstance)
								{
									var chr = (Character)ins;
									chr.IsLocalPlayer = true;
									var cam = new Camera(GameManager);
									cam.Parent = Root.GetService<Workspace>();
									cam.CameraSubject = chr;
								}
								if (loaded++ == sh.InstanceCount)
								{
									Root.GetService<CoreGui>().HideTeleportGui();
								}
							});
							con.RegisterRawDataHandler("nb.inc-service", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								ins.Parent = Root;
							});
							con.RegisterRawDataHandler("nb.placeinfo", (x, y) =>
							{
								sh = SerializationManager.DeserializeJsonBytes<ServerHandshake>(x.Data);

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

								Root.GetService<CoreGui>().ShowTeleportGui(sh.PlaceName, sh.Author, (int)sh.PlaceID, 0);

								y.SendRawData("nb.req-int-rep", []);
							});

							con.SendRawData("nb.handshake", SerializationManager.SerializeJsonBytes(ch));
						}
					}
					catch (Exception ex)
					{
						var msg = $"Could not connect to the server! {ex.GetType().Name} - {ex.Message}";
						var pls = Root.GetService<Players>();
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
		public string SeqFilterString(string text, Guid from, Guid to) => SeqFilterString(ServerConnection, text, from, to);
		public string SeqFilterString(Connection c, string text, Guid from, Guid to)
		{
			try
			{
				while (filtermutex) ;
				filtermutex = true;
				string? outp = null;
				var yes = false;
				var dss = new Dictionary<string, string>();

				dss["From"] = from.ToString();
				dss["To"] = to.ToString();
				dss["Text"] = text;

				c.SendRawData("nb.filter-string", SerializationManager.SerializeJsonBytes(dss));
				c.RegisterRawDataHandler("nb.filter-string-out", (x, y) =>
				{
					c.UnRegisterRawDataHandler("nb.filter-string-out");
					outp = x.ToUTF8String();
					yes = true;
				});

				while (!yes) ;
				return outp!;
			}
			catch
			{
				LogManager.LogError($"Failed to perform chat string filtering ({from}->{to})!");
				return "";
			}
		}
		public void SeqReparentInstance(Connection c, Instance ins)
		{
			try
			{
				var dss = new Dictionary<string, string>();
				dss["Instance"] = ins.UniqueID.ToString();
				dss["Parent"] = ins.ParentID.ToString();
				c.SendRawData("nb.repar-inst", SerializationManager.SerializeJsonBytes(dss));
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
					c.SendRawData("nb.inc-inst", SerializationManager.SerializeJsonBytes(dss));
				else
					c.SendRawData("nb.inc-service", SerializationManager.SerializeJsonBytes(dss));
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
				var ins = InstanceCreator.CreateInstance(inf["ClassName"], GameManager);
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
