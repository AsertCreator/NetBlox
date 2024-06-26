﻿using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Network;
using Network.Enums;
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
			public NetworkClient[] Recievers = [];
			public Instance Target;

			public const int REPM_TOALL = 0;
			public const int REPM_BUTOWNER = 1;
			public const int REPM_TORECIEVERS = 2;

			public const int REPW_NEWINST = 0;
			public const int REPW_PROPCHG = 1;
			public const int REPW_REPARNT = 2;

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
			public NetworkClient[] Recievers = [];
			public byte[] Data = [];
		}

		public GameManager GameManager;
		public List<NetworkClient> Clients = [];
		public Queue<Replication> ReplicationQueue = [];
		public Queue<RemoteEventPacket> RemoteEventQueue = [];
		public bool IsServer;
		public bool IsClient;
		public bool OnlyInternalConnections = false;
        public int ServerPort = 25570; // apparently that port was forbidden
		public Connection? RemoteConnection;
		public ServerConnectionContainer? Server;
		public CancellationTokenSource ClientReplicatorCanceller = new();
		public Task<object>? ClientReplicator;
		private object replock = new();
		private static Type LST = typeof(LuaSignal);
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
				LogManager.LogInfo(x.IPRemoteEndPoint.Address + " is trying to connect");
				bool gothandshake = false;

				x.RegisterRawDataHandler("nb2-handshake", (_x, _) =>
				{
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
					NetworkClient nc = null!;

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
                            Player pl = new(GameManager);
                            pl.IsLocalPlayer = false;
                            pl.Parent = Root.GetService<Players>();

                            nc = new(new(ch.Username), nextpid++, x, pl);

                            pl.Name = nc.Username;
							pl.Guest = isguest;
							pl.UserId = isguest ? Random.Shared.Next(-100000, -1) : userid;
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
							sh.InstanceCount = 2; // idk lol

							Clients.Add(nc);
						}
						else if (!stoppls)
						{
							sh.ErrorCode = 100;
						}
					}

					x.SendRawData("nb2-placeinfo", Encoding.UTF8.GetBytes(SerializationManager.SerializeJson(sh)));
					x.UnRegisterRawDataHandler("nb2-handshake");

					if (sh.ErrorCode != 0)
					{
						x.Close(CloseReason.ServerClosed);
						return;
					}

					void OnClose(CloseReason cr, Connection c)
					{
						LogManager.LogInfo(nc.Username + " had disconnected");
						if (nc.Player.Character != null)
							nc.Player.Character.Destroy();
						nc.Player.Destroy();
						Clients.Remove(nc);

						if (Clients.Count == 0)
							ReplicationQueue.Clear(); // we dont care anymore. we might as well shutdown hehe
					}

					x.ConnectionClosed += OnClose;

					bool acked = false;

					x.RegisterRawDataHandler("nb2-init", (_, _) =>
					{
						acked = true;

						AddReplication(Root.GetService<ReplicatedFirst>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<ReplicatedStorage>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Lighting>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Players>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterGui>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<StarterPack>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);
						AddReplication(Root.GetService<Workspace>(), Replication.REPM_TORECIEVERS, Replication.REPW_NEWINST, true, [nc]);

						x.RegisterRawDataHandler("nb2-replicate", (rep, _) =>
						{ // here we get instances from owners. actually for now, from everyone
							var ins = RecieveNewInstance(rep.Data);
							AddReplication(ins, Replication.REPM_BUTOWNER, Replication.REPW_NEWINST);
						});

						x.UnRegisterRawDataHandler("nb2-init"); // as per to constitution, we now do nothing lol
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

			// but actually we are not done

			while (!GameManager.ShuttingDown)
			{
				while (AppManager.BlockReplication) ;

				if (RemoteEventQueue.Count != 0)
					lock (RemoteEventQueue)
					{
						var re = RemoteEventQueue.Dequeue();
						var rc = re.Recievers;

						for (int i = 0; i < rc.Length; i++)
						{
							var c = rc[i];
							c.Connection.SendRawData("nb2-remote", re.RemoteEventId.ToByteArray().Concat(re.Data).ToArray()); // we dont care if it does not get sent
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
								rc = (NetworkClient[])Clients.ToArray().Clone();
								if (ins.NetworkOwner != null)
									rc.ToList().Remove(ins.NetworkOwner);
								break;
							case Replication.REPM_TORECIEVERS:
								break;
						}

						switch (rq.What)
						{
							case Replication.REPW_NEWINST:
								PerformReplicationNew(ins, rc); // not as per constitution, constitution is wrong, this cannot be implemented really
								break;
							case Replication.REPW_PROPCHG:
								PerformReplicationPropchg(ins, rc); // i found constitutional loophole, im not required to send only changed props, i can send entire instance bc idc
								break;
							case Replication.REPW_REPARNT:
								PerformReplicationReparent(ins, rc);
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
			RemoteConnection = tcp;

			ClientHandshake ch;
			ch.Username = GameManager.Username;
			ch.Authorization = SerializationManager.SerializeJson<Dictionary<string, string>>(new ()
			{
				["isguest"] = Profile.IsOffline ? "true" : "false",
				["userid"] = Profile.UserId.ToString()
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

			tcp.SendRawData("nb2-handshake", Encoding.UTF8.GetBytes(SerializationManager.SerializeJson(ch)));
			tcp.RegisterRawDataHandler("nb2-placeinfo", (x, _) =>
			{
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

				Root.UniqueID = Guid.Parse(sh.DataModelInstance);
				Root.Name = sh.PlaceName;
				Root.Clear();

				// i feel some netflix ce exploit shit can be done here.

				int actinstc = sh.InstanceCount - 2; // didn't i just tell the server that optimal instance count is 2, so here its 0?
				int gotinsts = 0;
				Camera c = new(GameManager);
				Player? lp = null;

				tcp.RegisterRawDataHandler("nb2-replicate", (rep, _) =>
				{
					if (++gotinsts >= actinstc)
						Root.GetService<CoreGui>().HideTeleportGui();

					var ins = RecieveNewInstance(rep.Data);

					if (ins is Workspace)
					{
						((Workspace)ins).MainCamera = c; // I FORGOR THAT I ALREADY HAD A Camera PROPERTY
						c.Parent = ins;
					}
					if (ins is Character && Guid.Parse(sh.CharacterInstance) == ins.UniqueID) // i hope FOR THE JESUS CHRIST, that the Player instance had been delivered before the character
					{
						var ch = (Character)ins;
						ch.IsLocalPlayer = true;
						c.CameraSubject = ch;
						if (lp != null)
							lp.Character = ch;
					}
					if (ins is Player && Guid.Parse(sh.PlayerInstance) == ins.UniqueID)
					{
						lp = (Player)ins;
						lp.IsLocalPlayer = true;
						Root.GetService<Players>().CurrentPlayer = lp;
					}
				});
				tcp.RegisterRawDataHandler("nb2-reparent", (rep, _) =>
				{
					Guid inst = new Guid(rep.Data[0..16]);
					Guid newp = new Guid(rep.Data[16..32]);
					Instance? actinst = GameManager.GetInstance(inst);
					if (actinst != null)
					{
						Instance? parent = GameManager.GetInstance(inst);
						if (parent != null)
							actinst.Parent = parent;
						else
							actinst.Destroy();
					}
				});
				tcp.RegisterRawDataHandler("nb2-kick", (rep, _) =>
				{
					tcp.ConnectionClosed -= OnClose;
					if (GameManager.RenderManager != null)
						GameManager.RenderManager.ShowKickMessage(Encoding.UTF8.GetString(rep.Data));
				});
				tcp.SendRawData("nb2-init", []);
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
						var rc = re.Recievers;

						for (int i = 0; i < rc.Length; i++)
						{
							var c = rc[i];
							c.Connection.SendRawData("nb2-remote", re.RemoteEventId.ToByteArray().Concat(re.Data).ToArray()); // we dont care if it does not get sent
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
								rc = (NetworkClient[])Clients.ToArray().Clone();
								if (ins.NetworkOwner != null)
									rc.ToList().Remove(ins.NetworkOwner);
								break;
							case Replication.REPM_TORECIEVERS:
								break;
						}

						switch (rq.What)
						{
							case Replication.REPW_NEWINST:
								PerformReplicationNew(ins, rc); // not as per constitution, constitution is wrong, this cannot be implemented really
								break;
							case Replication.REPW_PROPCHG:
								PerformReplicationPropchg(ins, rc); // i found constitutional loophole, im not required to send only changed props, i can send entire instance bc idc
								break;
							case Replication.REPW_REPARNT:
								PerformReplicationReparent(ins, rc);
								break;
						}
					}
			}
		}
		public void PerformKick(NetworkClient? nc, string msg, bool islocal)
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
			if (nc == null) throw new Exception("NetworkClient object not preserved!");
			nc.Connection.SendRawData("nb2-kick", Encoding.UTF8.GetBytes(msg));
			nc.Connection.Close(CloseReason.ServerClosed);
		}
		private void PerformReplicationNew(Instance ins, NetworkClient[] recs)
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);
			var gm = ins.GameManager;
			var type = ins.GetType(); // apparently gettype caches type object but i dont believe
			var props = type.GetProperties();

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

				con.SendRawData("nb2-replicate", buf);
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
				if (ins == null)
				{
					if (GameManager.FilteringEnabled && IsServer)
						return null!; // we do not permit this shit.
					ins = InstanceCreator.CreateReplicatedInstance(br.ReadString(), GameManager);
				}
				ins.UniqueID = guid;
				ins.WasReplicated = true;
				var type = ins.GetType();

				ins.Parent = GameManager.GetInstance(newp);

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
		private void PerformReplicationPropchg(Instance ins, NetworkClient[] recs)
		{
			PerformReplicationNew(ins, recs); // same thing really
		}
		private unsafe void PerformReplicationReparent(Instance ins, NetworkClient[] recs)
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

				con.SendRawData("nb2-reparent", b);
			}
		}
		public void AddReplication(Instance inst, int m, int w, bool rc = true, NetworkClient[]? nc = null)
		{ // the fucking aRgUmEnT ExCePtIoN CirCumCiSiTiOn .net fuck off for god's sake
			lock (ReplicationQueue)
			{
				AddReplicationImpl(inst, m, w, rc, nc);
			}
		}
		private void AddReplicationImpl(Instance inst, int m, int w, bool rc = true, NetworkClient[]? nc = null)
		{
			Thread.Sleep(1); // i just cant
			ReplicationQueue.Enqueue(new(m, w, inst)
			{
				Recievers = nc ?? []
			});
			if (rc)
				for (int i = 0; i < inst.Children.Count; i++)
					AddReplicationImpl(inst.Children[i], m, w, true, nc);
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
