using MoonSharp.Interpreter;
using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using Network.Enums;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using CloseReason = Network.Enums.CloseReason;

namespace NetBlox
{
	public sealed class NetworkManager
	{
		private unsafe struct ClientHandshake
		{
			public string Username;
			public string Authorization;
			public ushort VersionMajor;
			public ushort VersionMinor;
			public ushort VersionPatch;
		}
		private unsafe struct ServerHandshake
		{
			public string PlaceName;
			public string UniverseName;
			public string Author;

			public ulong PlaceID;
			public ulong UniverseID;
			public uint MaxPlayerCount;
			public uint UniquePlayerID;

			public int ErrorCode;

			public int InstanceCount;
			public string DataModelInstance;
			public string PlayerInstance;
			public string CharacterInstance;
		}
		public class Replication
		{
			public int Mode;
			public int What;
			public RemoteClient[] Recievers = [];
			public string[] Properties = [];
			public Instance Target;

			public const int REPM_TOALL = 0;
			public const int REPM_BUTOWNER = 1;
			public const int REPM_TORECIEVERS = 2;

			public const int REPW_NEWINST = 0;
			public const int REPW_PROPCHG = 1;
			public const int REPW_REPARNT = 2;
			public const int REPW_DESTROY = 3;

			public Replication(int m, int w, Instance t)
			{
				Mode = m;
				What = w;
				Target = t;
			}
		}
		public class RemoteEventPacket
		{
			public Guid RemoteEventId;
			public RemoteClient[] Recievers = [];
			public byte[] Data = [];
		}
		public class ChatMessageData
		{
			public Player Player;
			public string Message;
		}

		public GameManager GameManager;
		public List<RemoteClient> Clients = [];
		public Queue<ChatMessageData> ChatMessages = [];
		public Queue<Replication> ReplicationQueue = [];
		public Queue<RemoteEventPacket> RemoteEventQueue = [];
		public string? ChatMessage;
		public bool IsServer;
		public bool IsClient;
		public bool IsLoaded = true;
		public bool OnlyInternalConnections = false;
		public int ServerPort = 25570; // apparently that port was forbidden
		public int OutgoingTraffic = 0;
		public int IncomingTraffic = 0;
		public Connection? RemoteConnection;
		public ServerConnectionContainer? Server;
		public CancellationTokenSource ClientReplicatorCanceller = new();
		public Task<object>? ClientReplicator;
		private object replock = new();
		private static Type LST = typeof(LuaSignal);
		private bool profilerDebug = false;
		private int outgoingPacketsSent = 0;
		private int incomingPacketsRecieved = 0;
		private int outgoingTraffic = 0;
		private int incomingTraffic = 0;
		private DataModel Root => GameManager.CurrentRoot;
		private uint nextpid = 0;
		private bool init;

		public NetworkManager(GameManager gm, bool server, bool client)
		{
			GameManager = gm;
			if (!init)
			{
				IsServer = server;
				IsClient = client;
				if (IsServer)
					ServerPort = (GameManager.ServerStartupInfo ?? throw new Exception()).ServerPort;
				StartProfiling(true);
				init = true;
			}
		}
		/// <summary>
		/// Starts NetBlox server on current <seealso cref="NetworkManager"/>, this function is blocking, start it in other thread.
		/// </summary>
		public unsafe void StartServer()
		{
			if (!IsServer)
				throw new NotSupportedException("Cannot start server in non-server configuration!");

			Server = ConnectionFactory.CreateServerConnectionContainer(ServerPort);
			Server.AllowUDPConnections = false;
			Server.ConnectionEstablished += (x, y) =>
			{
				x.EnableLogging = false;
				x.KeepAlive = !Debugger.IsAttached;
				LogManager.LogInfo(x.IPRemoteEndPoint.Address + " is trying to connect");
				bool gothandshake = false;

				x.RegisterRawDataHandler("nb2-handshake", (_x, _) =>
				{
					ProfileIncoming(_x.Key, _x.Data);

					byte[] data = _x.Data;
					ClientHandshake ch = SerializationManager.DeserializeJson<ClientHandshake>(Encoding.UTF8.GetString(data));

					gothandshake = true;

					// as per to constitution, we must immediately disconnect the client if version mismatch happens

					if (ch.VersionMajor != Common.Version.VersionMajor ||
						ch.VersionMinor != Common.Version.VersionMinor ||
						ch.VersionPatch != Common.Version.VersionPatch)
					{
						LogManager.LogWarn(x.IPRemoteEndPoint.Address + " has wrong version! disconnecting...");
						x.Close(CloseReason.DifferentVersion);
						return;
					}

					ServerHandshake sh = new();
					RemoteClient nc = null!;
					Player pl = null!;

					// here we do a lot of shit
					{
						sh.Author = GameManager.CurrentIdentity.Author;
						sh.PlaceName = GameManager.CurrentIdentity.PlaceName;
						sh.UniverseName = GameManager.CurrentIdentity.UniverseName;

						sh.ErrorCode = 0;

						bool stoppls = false;
						bool isguest = false;
						long userid = -1;
						string str = x.IPRemoteEndPoint.Address.ToString();

						Dictionary<string, string> authdata = SerializationManager.DeserializeJson<Dictionary<string, string>>(ch.Authorization);
						if (authdata == null)
						{
							LogManager.LogWarn(x.IPRemoteEndPoint.Address + " didn't pass authorization data! disconnecting...");
							sh.ErrorCode = 102;
							stoppls = true;
							return;
						}
						if (authdata.TryGetValue("isguest", out string val) && bool.Parse(val)) isguest = true;
						if (authdata.TryGetValue("userid", out string uid)) userid = long.Parse(uid);

						if (!(isguest ^ (userid != -1)))
						{
							LogManager.LogWarn(x.IPRemoteEndPoint.Address + "'s authorization data contains both guest and account data! disconnecting...");
							sh.ErrorCode = 102;
							stoppls = true;
							return;
						}

						if (OnlyInternalConnections && str != "::ffff:127.0.0.1")
						{
							sh.ErrorCode = 101;
							stoppls = true;
						}
						if (Clients.Count < GameManager.CurrentIdentity.MaxPlayerCount && !stoppls)
						{
							pl = new(GameManager);
							pl.IsLocalPlayer = false;
							pl.Parent = Root.GetService<Players>();

							nc = new(new(ch.Username), nextpid++, x, pl);

							pl.Name = nc.Username;
							pl.Guest = isguest;
							pl.SetUserId(isguest ? Random.Shared.Next(-100000, -1) : userid);
							pl.CharacterAppearanceId = pl.UserId;
							pl.Client = nc;
							nc.Player = pl;

							pl.Reload();
							pl.LoadCharacterOld();

							sh.PlayerInstance = pl.UniqueID.ToString();
							sh.CharacterInstance = pl.Character!.UniqueID.ToString();
							sh.DataModelInstance = Root.UniqueID.ToString();
							sh.MaxPlayerCount = GameManager.CurrentIdentity.MaxPlayerCount;
							sh.UniverseID = GameManager.CurrentIdentity.UniverseID;
							sh.PlaceID = GameManager.CurrentIdentity.PlaceID;
							sh.UniquePlayerID = nc.UniquePlayerID;
							sh.InstanceCount = 
								Root.GetService<Workspace>().CountDescendants() +
								Root.GetService<ReplicatedStorage>().CountDescendants() +
								Root.GetService<ReplicatedFirst>().CountDescendants() +
								Root.GetService<Chat>().CountDescendants() +
								Root.GetService<Lighting>().CountDescendants();

							Clients.Add(nc);
						}
						else if (!stoppls)
						{
							sh.ErrorCode = 100;
						}
					}

					SendRawData(x, "nb2-placeinfo", Encoding.UTF8.GetBytes(SerializationManager.SerializeJson(sh)));
					x.UnRegisterRawDataHandler("nb2-handshake");

					if (sh.ErrorCode != 0)
					{
						x.Close(CloseReason.ServerClosed);
						return;
					}

					void OnClose(CloseReason cr, Connection c)
					{
						LogManager.LogInfo(nc.Username + " had disconnected");
						nc.Player.Character?.Destroy();
						nc.Player.Destroy();
						Clients.Remove(nc);

						if (Clients.Count == 0) 
						{ 
							ReplicationQueue.Clear(); // we dont care anymore. we might as well shutdown hehe
							GameManager.Shutdown();
						}
					}

					x.ConnectionClosed += OnClose;

					bool acked = false;

					x.RegisterRawDataHandler("nb2-init", (rep, _) =>
					{
						ProfileIncoming(rep.Key, rep.Data);

						acked = true;

						AddReplication(Root.GetService<ReplicatedFirst>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<ReplicatedStorage>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Chat>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Lighting>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Players>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterGui>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterPack>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Workspace>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);

						Debug.Assert(pl.Character != null);

						AddReplication(pl, Replication.REPM_TOALL, Replication.REPW_NEWINST, false);
						AddReplication(pl.Character, Replication.REPM_TOALL, Replication.REPW_NEWINST, true);

						x.RegisterRawDataHandler("nb2-replicate", (rep, _) =>
						{ // here we get instances from owners. actually for now, from everyone
							ProfileIncoming(rep.Key, rep.Data);

							var ins = RecieveNewInstance(rep.Data);
							AddReplication(ins, Replication.REPM_BUTOWNER, Replication.REPW_NEWINST);
						});
						x.RegisterRawDataHandler("nb2-remote", (rep, _) =>
						{
							ProfileIncoming(rep.Key, rep.Data);

							Guid inst = new Guid(rep.Data[0..16]);
							RemoteEvent even = GameManager.GetInstance(inst) as RemoteEvent;
							if (even == null)
								return; // womp womp. what the heck in fact? is this fooker exploiting?
							byte[] payload = rep.Data[16..];
							DynValue luadata = SerializationManager.DeserializeObject(payload, GameManager);

							even.OnServerEvent.Fire(DynValue.NewTable(LuaRuntime.MakeInstanceTable(pl, GameManager)), luadata);
						});
						x.RegisterRawDataHandler("nb2-chat", (rep, _) =>
						{
							ProfileIncoming(rep.Key, rep.Data);

							Chat service = Root.GetService<Chat>();
							service.ProcessMessage(pl, Encoding.UTF8.GetString(rep.Data));
						});

						x.UnRegisterRawDataHandler("nb2-init"); // as per to constitution, we now do nothing lol
					});
					x.RegisterRawDataHandler("nb2-ownerreplicate", (data, _) =>
					{
						ProfileIncoming(data.Key, data.Data);

						Instance ins = RecieveNewInstance(data.Data);
						for (int i = 0; i < Clients.Count; i++)
						{
							RemoteClient nc2 = Clients[i];
							if (nc2 != nc)
								nc2.Connection.SendRawData("nb2-replicate", data.Data);
						}
					});
					x.RegisterRawDataHandler("nb2-gotchar", (data, _) =>
					{
						ProfileIncoming(data.Key, data.Data);

						Guid inst = new Guid(data.Data);
						Instance? actinst = GameManager.GetInstance(inst);
						actinst?.SetNetworkOwner(pl);
					});

					Task.Delay(5000).ContinueWith(_ =>
					{
						if (!acked)
						{
							LogManager.LogWarn(x.IPRemoteEndPoint.Address + " didn't acknowledge server handshake! disconnecting...");
							x.Close(CloseReason.NetworkError);
							return;
						}
					});
				});

				Task.Delay(5000).ContinueWith(_ =>
				{
					if (!gothandshake)
					{
						LogManager.LogWarn(x.IPRemoteEndPoint.Address + " didn't send handshake! disconnecting...");
						x.Close(CloseReason.NetworkError);
						return;
					}
				});
			};

			Server.Start();
			GameManager.AllowReplication = true;

			LogManager.LogInfo($"Listening at {Server.IPAddress}:{ServerPort}");

			// but actually we are not done

			while (!GameManager.ShuttingDown)
			{
				while (AppManager.BlockReplication) ;

				if (RemoteEventQueue.Count != 0)
					lock (RemoteEventQueue)
					{
						var re = RemoteEventQueue.Dequeue();
						var rc = re.Recievers;
						var payload = re.RemoteEventId.ToByteArray().Concat(re.Data).ToArray();

						for (int i = 0; i < rc.Length; i++)
						{
							var c = rc[i];
							SendRawData(c.Connection, "nb2-remote", payload); // we dont care if it does not get sent
						}
					}
				if (ChatMessages.Count != 0)
					lock (ChatMessages)
					{
						var cmd = ChatMessages.Dequeue();
						var rc = Clients.ToArray();
						var payload = cmd.Player.UniqueID.ToByteArray().Concat(Encoding.UTF8.GetBytes(cmd.Message)).ToArray();

						for (int i = 0; i < rc.Length; i++)
						{
							var c = rc[i];
							SendRawData(c.Connection, "nb2-chat", payload);
						}
					}
				if (ReplicationQueue.Count != 0)
					lock (ReplicationQueue)
					{
						var rq = ReplicationQueue.Dequeue();
						var rc = rq.Recievers;
						var ins = rq.Target;

						switch (rq.Mode)
						{
							case Replication.REPM_TOALL:
								rc = Clients.ToArray();
								break;
							case Replication.REPM_BUTOWNER:
								rc = (RemoteClient[])(Clients.ToArray().Clone());
								var oq = (from x in GameManager.Owners where x.Value == ins select x.Key).FirstOrDefault();
								if (oq != null)
								{
									var lis = rc.ToList();
									lis.Remove(oq);
									rc = lis.ToArray();
								}
								break;
							case Replication.REPM_TORECIEVERS:
								break;
						}

						if (rc.Length == 0) continue;

						switch (rq.What)
						{
							case Replication.REPW_NEWINST:
								PerformReplicationPropchg(ins, SerializationManager.GetAccessibleProperties(ins), rc);
								break;
							case Replication.REPW_PROPCHG:
								PerformReplicationPropchg(ins, rq.Properties, rc); // all of a sudden i do care now
								break;
							case Replication.REPW_REPARNT:
								PerformReplicationReparent(ins, rc);
								break;
							case Replication.REPW_DESTROY:
								PerformReplicationDestroy(ins, rc);
								break;
						}
					}
			}
		}
		public unsafe void ConnectToServer(IPAddress ipa)
		{
			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			var cn = ConnectionResult.TCPConnectionNotAlive;
			var tcp = ConnectionFactory.CreateTcpConnection(ipa.ToString(), ServerPort, out cn);
			if (tcp == null)
				throw new Exception("Remote server had refused to connect");
			tcp.EnableLogging = false;
			tcp.KeepAlive = !Debugger.IsAttached;
			RemoteConnection = tcp;

			ClientHandshake ch;
			ch.Username = GameManager.Username;
			ch.Authorization = SerializationManager.SerializeJson<Dictionary<string, string>>(new ()
			{
				["isguest"] = GameManager.CurrentProfile.IsOffline ? "true" : "false",
				["userid"] = GameManager.CurrentProfile.UserId.ToString()
			});
			ch.VersionMajor = Common.Version.VersionMajor;
			ch.VersionMinor = Common.Version.VersionMinor;
			ch.VersionPatch = Common.Version.VersionPatch;

			void OnClose(CloseReason cr, Connection c)
			{
				if (GameManager.RenderManager != null)
					GameManager.RenderManager.ShowKickMessage("The server had closed");
				GameManager.IsRunning = false;
			}

			bool gotpi = false;

			SendRawData(tcp, "nb2-handshake", Encoding.UTF8.GetBytes(SerializationManager.SerializeJson(ch)));
			tcp.RegisterRawDataHandler("nb2-placeinfo", (x, _) =>
			{
				ProfileIncoming(x.Key, x.Data);

				gotpi = true;
				tcp.UnRegisterRawDataHandler("nb2-placeinfo");
				ServerHandshake sh = SerializationManager.DeserializeJson<ServerHandshake>(Encoding.UTF8.GetString(x.Data));

				if (sh.ErrorCode != 0)
					throw new Exception(TranslateErrorCode(sh.ErrorCode));

				tcp.ConnectionClosed += OnClose;

				GameManager.CurrentIdentity.PlaceName = sh.PlaceName;
				GameManager.CurrentIdentity.UniverseName = sh.UniverseName;
				GameManager.CurrentIdentity.PlaceID = sh.PlaceID;
				GameManager.CurrentIdentity.UniverseID = sh.UniverseID;
				GameManager.CurrentIdentity.UniquePlayerID = sh.UniquePlayerID;
				GameManager.CurrentIdentity.Author = sh.Author;
				GameManager.CurrentIdentity.MaxPlayerCount = sh.MaxPlayerCount;

				Root.GetService<CoreGui>().ShowTeleportGui(
					GameManager.CurrentIdentity.PlaceName, GameManager.CurrentIdentity.Author,
					(int)GameManager.CurrentIdentity.PlaceID, (int)GameManager.CurrentIdentity.UniverseID);
				IsLoaded = false;

				Root.UniqueID = Guid.Parse(sh.DataModelInstance);
				Root.Name = sh.PlaceName;
				Task.Run(GameManager.CurrentRoot.Clear);

				// i feel some netflix ce exploit shit can be done here.

				int actinstc = sh.InstanceCount - 4;
				int gotinsts = 0;
				Player? lp = null;

				tcp.RegisterRawDataHandler("nb2-replicate", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					if (++gotinsts >= actinstc)
					{
						Root.GetService<CoreGui>().HideTeleportGui();
						IsLoaded = true;
					}

					TaskScheduler.ScheduleNetwork("RecieveInstance", GameManager, x =>
					{
						var ins = RecieveNewInstance(rep.Data);

						if (ins is Workspace)
						{
							Camera c = new(GameManager);
							((Workspace)ins).MainCamera = c; // I FORGOR THAT I ALREADY HAD A Camera PROPERTY
							c.Parent = ins;
						}
						if (ins is Character && Guid.Parse(sh.CharacterInstance) == ins.UniqueID) // i hope FOR THE JESUS CHRIST, that the Player instance had been delivered before the character
						{
							var cam = Root.GetService<Workspace>().MainCamera as Camera;
							var ch = (Character)ins;
							ch.IsLocalPlayer = true;

							if (cam == null)
								return JobResult.CompletedFailure; // hehe

							cam.CameraSubject = ch;
							if (lp != null)
							{
								lp.Character = ch;
								SendRawData(tcp, "nb2-gotchar", ch.UniqueID.ToByteArray());
							}
						}
						if (ins is Player && Guid.Parse(sh.PlayerInstance) == ins.UniqueID)
						{
							lp = (Player)ins;
							lp.IsLocalPlayer = true;
							Root.GetService<Players>().CurrentPlayer = lp;
						}

						return JobResult.CompletedSuccess;
					});
				});
				tcp.RegisterRawDataHandler("nb2-remote", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Guid inst = new Guid(rep.Data[0..16]);
					RemoteEvent even = GameManager.GetInstance(inst) as RemoteEvent;
					if (even == null)
						return; // womp womp
					byte[] payload = rep.Data[16..];
					DynValue luadata = SerializationManager.DeserializeObject(payload, GameManager);

					even.OnClientEvent.Fire(luadata);
				});
				tcp.RegisterRawDataHandler("nb2-chat", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Player plr = GameManager.GetInstance(new Guid(rep.Data[0..16])) as Player;
					string msg = Encoding.UTF8.GetString(rep.Data[16..]);
					Chat service = Root.GetService<Chat>(true);

					if (service == null || plr == null)
						return; // welp

					service.ProcessMessage(plr, msg);
				});
				tcp.RegisterRawDataHandler("nb2-reparent", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Guid inst = new Guid(rep.Data[0..16]);
					Guid newp = new Guid(rep.Data[16..32]);
					Instance? actinst = GameManager.GetInstance(inst);
					if (actinst != null)
					{
						Instance? parent = GameManager.GetInstance(inst);
						actinst.Parent = parent;
					}
				});
				tcp.RegisterRawDataHandler("nb2-destroy", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Guid inst = new Guid(rep.Data);
					Instance? actinst = GameManager.GetInstance(inst);
					if (actinst != null)
					{
						actinst.Destroy();
					}
				});
				tcp.RegisterRawDataHandler("nb2-setowner", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Guid inst = new Guid(rep.Data);
					Instance? actinst = GameManager.GetInstance(inst);
					if (actinst != null)
					{
						GameManager.SelfOwnerships.Add(actinst);
					}
				});
				tcp.RegisterRawDataHandler("nb2-confiscate", (rep, _) =>
				{
					ProfileIncoming(rep.Key, rep.Data);

					Guid inst = new Guid(rep.Data);
					Instance? actinst = GameManager.GetInstance(inst);
					if (actinst != null)
					{
						GameManager.SelfOwnerships.Remove(actinst);
					}
				});
				tcp.RegisterRawDataHandler("nb2-kick", (rep, _) =>
				{
					tcp.ConnectionClosed -= OnClose;
					if (GameManager.RenderManager != null)
						GameManager.RenderManager.ShowKickMessage(Encoding.UTF8.GetString(rep.Data));
				});
				SendRawData(tcp, "nb2-init", []);
			});

			Task.Run(() =>
			{
				for (int i = 0; i < 7000 && !gotpi; i++)
				{
					if (!tcp.IsAlive)
						throw new Exception("Your NetBlox client is outdated!");
					Thread.Sleep(1);
				}
			});

			while (!GameManager.ShuttingDown)
			{
				while (AppManager.BlockReplication) ;

				if (RemoteEventQueue.Count != 0)
					lock (RemoteEventQueue)
					{
						var re = RemoteEventQueue.Dequeue();
						SendRawData(RemoteConnection, "nb2-remote", re.RemoteEventId.ToByteArray().Concat(re.Data).ToArray());
					}
				if (ReplicationQueue.Count != 0)
					lock (ReplicationQueue)
					{
						var rq = ReplicationQueue.Dequeue();
						var ins = rq.Target;

						switch (rq.What)
						{
							case Replication.REPW_PROPCHG:
								Type t = ins.GetType();
								List<PropertyInfo> pis = (from x in rq.Properties select t.GetProperty(x)).ToList();
								RequestOwnerReplication(ins, pis.ToArray());
								break;
						}
					}
				if (ChatMessage != null) 
				{ 
					SendRawData(RemoteConnection, "nb2-chat", Encoding.UTF8.GetBytes(ChatMessage)); // we dont care if it does not get sent
					ChatMessage = null;
				}
			}
		}
		public void StartProfiling(bool dbg)
		{
			profilerDebug = dbg;
			Task.Run(() =>
			{
				while (!GameManager.ShuttingDown)
				{
					Thread.Sleep(1000);
					OutgoingTraffic = outgoingTraffic;
					outgoingTraffic = 0;
					IncomingTraffic = incomingTraffic;
					incomingTraffic = 0;
				}
			});
		}
		public void ProfileIncoming(string key, byte[] data)
		{
			var len = Encoding.ASCII.GetBytes(key).Length + data.Length;
			Debug.WriteLine($"!! nmprofiler, INCOMING #{incomingPacketsRecieved++}, key: {key}, data len: {data.Length}, incoming bytes/sec: {IncomingTraffic} !!");
			incomingTraffic += len;
		}
		public void ProfileOutgoing(string key, byte[] data)
		{
			var len = Encoding.ASCII.GetBytes(key).Length + data.Length;
			Debug.WriteLine($"!! nmprofiler, OUTGOING #{outgoingPacketsSent++}, key: {key}, data len: {data.Length}, outgoing bytes/sec: {OutgoingTraffic} !!");
			outgoingTraffic += len;
		}
		public void SendRawData(Connection c, string key, byte[] data)
		{
			ProfileOutgoing(key, data);
			c.SendRawData(key, data);
		}
		public void Confiscate(Instance ins)
		{
			var query = (from x in GameManager.Owners where x.Value == ins select x);
			if (query.Any())
			{
				var ns = query.First().Key;
				GameManager.Owners.Remove(ns);
				SendRawData(ns.Connection, "nb2-confiscate", ins.UniqueID.ToByteArray());
			} 
		}
		public void SetOwner(RemoteClient nc, Instance ins)
		{
			GameManager.Owners[nc] = ins;
			SendRawData(nc.Connection, "nb2-setowner", ins.UniqueID.ToByteArray());
		}
		public void RequestOwnerReplication(Instance ins, PropertyInfo[] props)
		{
			if (RemoteConnection == null) return;

			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);
			var gm = ins.GameManager;
			var type = ins.GetType(); // apparently gettype caches type object but i dont believe

			bw.Write(ins.UniqueID.ToByteArray());
			bw.Write(ins.ParentID.ToByteArray());
			bw.Write(""); // we dont actually

			var c = 0;

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];

				if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
					continue;
				if (prop.PropertyType == LST)
					continue;
				if (!prop.CanWrite)
					continue;
				if (!SerializationManager.NetworkSerializers.TryGetValue(prop.PropertyType.FullName ?? "", out var x))
					continue;

				var v = prop.GetValue(ins);
				if (v == null) continue;
				c++;
				var b = x(v, gm);

				bw.Write(prop.Name);
				bw.Write((short)b.Length);
				bw.Write(b);
			}

			bw.Write((byte)c);

			byte[] buf = ms.ToArray();

			SendRawData(RemoteConnection, "nb2-ownerreplicate", buf);
		}
		public void PerformKick(RemoteClient? nc, string msg, bool islocal)
		{
			// it's not really constitutionally defined, but idc.
			if (RemoteConnection == null) return;
			if (IsClient && !islocal)
				throw new Exception("Cannot kick non-local player from client");
			if (IsClient && islocal)
			{
				RemoteConnection.Close(CloseReason.ClientClosed);
				if (GameManager.RenderManager != null)
					GameManager.RenderManager.ShowKickMessage(msg);
			}

			// we are on server
			if (nc == null) throw new Exception("RemoteClient object not preserved!");
			SendRawData(nc.Connection, "nb2-kick", Encoding.UTF8.GetBytes(msg));
			nc.Connection.Close(CloseReason.ServerClosed);
		}
		private void PerformReplicationNew(Instance ins, PropertyInfo[] props, RemoteClient[] recs)
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);
			var gm = ins.GameManager;
			var type = ins.GetType(); // apparently gettype caches type object but i dont believe

			bw.Write(ins.UniqueID.ToByteArray());
			bw.Write(ins.ParentID.ToByteArray());
			bw.Write(ins.ClassName);

			var c = 0;

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];

				if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
					continue;
				if (prop.PropertyType == LST)
					continue;
				if (!prop.CanWrite)
					continue;
				if (!SerializationManager.NetworkSerializers.TryGetValue(prop.PropertyType.FullName ?? "", out var x))
					continue;

				var v = prop.GetValue(ins);
				if (v == null) continue;
				c++;
				var b = x(v, gm);

				bw.Write(prop.Name);
				bw.Write((short)b.Length);
				bw.Write(b);
			}

			bw.Write((byte)c);

			byte[] buf = ms.ToArray();

			for (int i = 0; i < recs.Length; i++)
			{
				var nc = recs[i];
				var con = nc.Connection;
				if (con == null) continue; // how did this happen

				SendRawData(con, "nb2-replicate", buf);
			}
		}
		private Instance RecieveNewInstance(byte[] data)
		{
			lock (replock)
			{
				using MemoryStream ms = new(data);
				using BinaryReader br = new(ms);

				int propc = data[data.Length - 1];
				Guid guid = new(br.ReadBytes(16));
				Guid newp = new(br.ReadBytes(16));
				var ins = GameManager.GetInstance(guid);
				var classname = br.ReadString();
				if (ins == null)
				{
					if (GameManager.FilteringEnabled && IsServer)
						return null!; // we do not permit this shit.
					ins = InstanceCreator.CreateReplicatedInstance(classname, GameManager);
					ins.Parent = GameManager.GetInstance(newp);
				}
				ins.UniqueID = guid;
				ins.WasReplicated = true;
				var type = ins.GetType();

				for (int i = 0; i < propc; i++)
				{
					string propname = br.ReadString();
					var prop = type.GetProperty(propname);
					if (prop == null) // how did this happen
						continue;

					var ptyp = prop.PropertyType;
					var pnam = ptyp.FullName ?? "";
					var bc = br.ReadInt16();
					var pbytes = br.ReadBytes(bc);

					if (SerializationManager.NetworkDeserializers.TryGetValue(pnam, out var x) && prop.CanWrite)
						prop.SetValue(ins, x(pbytes, GameManager));
				}

				GameManager.IsRunning = true; // i cant find better place
				return ins;
			}
		}
		private void PerformReplicationPropchg(Instance ins, string[] props, RemoteClient[] recs)
		{
			Type t = ins.GetType();
			List<PropertyInfo> pis = (from x in props select t.GetProperty(x)).ToList();
			PerformReplicationNew(ins, pis.ToArray(), recs); // same thing really
		}
		private unsafe void PerformReplicationReparent(Instance ins, RemoteClient[] recs)
		{
			var bytes = new List<byte>();
			bytes.AddRange(ins.UniqueID.ToByteArray());
			bytes.AddRange(ins.ParentID.ToByteArray());
			var b = bytes.ToArray();

			for (int i = 0; i < recs.Length; i++)
			{
				var nc = recs[i];
				var con = nc.Connection;
				if (con == null) continue; // how did this happen

				SendRawData(con, "nb2-reparent", b);
			}
		}
		public void PerformReplicationDestroy(Instance ins, RemoteClient[] recs)
		{
			var b = ins.UniqueID.ToByteArray();

			for (int i = 0; i < recs.Length; i++)
			{
				var nc = recs[i];
				var con = nc.Connection;
				if (con == null) continue; // how did this happen

				SendRawData(con, "nb2-destroy", b);
			}
		}
		public Replication? AddReplication(Instance inst, int m, int w, bool rc = true, RemoteClient[]? nc = null)
		{ // the fucking aRgUmEnT ExCePtIoN CirCumCiSiTiOn .net fuck off for god's sake
			lock (ReplicationQueue)
			{
				return AddReplicationImpl(inst, m, w, rc, nc);
			}
		}
		private Replication? AddReplicationImpl(Instance inst, int m, int w, bool rc = true, RemoteClient[]? nc = null)
		{
			if (inst is ServerStorage) return null;
			if (inst is Camera) return null; // worky arounds

			var rep = new Replication(m, w, inst)
			{
				Recievers = nc ?? []
			};
			ReplicationQueue.Enqueue(rep);
			if (rc)
				for (int i = 0; i < inst.Children.Count; i++)
					AddReplicationImpl(inst.Children[i], m, w, true, nc);
			return rep;
		}
		public static string TranslateErrorCode(int ec)
		{
			switch (ec)
			{
				case 100:
					return "The server is full";
				case 101:
					return "Server only accepts internal connections";
				case 102:
					return "Authorization failed";
				case 103:
					return "A player with same name is already playing";
				case 104:
					return "Server is just being weird";
				default:
					return "Unknown connection failure";
			}
		}
	}
}
