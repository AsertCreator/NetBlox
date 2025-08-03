using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Network;
using Network;
using System.Diagnostics;
using System.Net;
using CloseReason = Network.Enums.CloseReason;
using System.Numerics;

namespace NetBlox
{
	public sealed class NetworkManager
	{
		public readonly static Type InstanceType = typeof(Instance);
		public readonly static Type LST = typeof(LuaSignal);

		public GameManager GameManager;
		public List<RemoteClient> Clients = [];
		public bool IsServer;
		public bool IsClient;
		public bool IsLoaded = true;
		public bool OnlyInternalConnections = false;
		public bool NetworkProfilerLog = false;
		public int ServerPort = 25570; // apparently that port was forbidden
		public int OutgoingTraffic = 0;
		public int IncomingTraffic = 0;
		public int LocalBufferZoneLimits = 0;
		public Vector3 LocalBufferZoneCenter = default;
		public Guid ExpectedLocalPlayerGuid = default;

		public Queue<Replication> ReplicationQueue = [];
		public List<(Guid, Action)> AwaitingForArrival = [];

		public Connection? RemoteConnection;
		public ServerConnectionContainer? Server;
		public CancellationTokenSource ClientReplicatorCanceller = new();
		public Task<object>? ClientReplicator;

		public int LoadedInstanceCount;
		public int TargetInstanceCount;
		public bool SynchronousReplication = true;

		private readonly static object replock = new();
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
				StartProfiling();
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
			Server.ConnectionEstablished += (connection, y) =>
			{
				connection.EnableLogging = false;
				connection.KeepAlive = !Debugger.IsAttached;

				LogManager.LogInfo(connection.IPRemoteEndPoint.Address + " is trying to connect");

				var remoteclient = new RemoteClient(GameManager, nextpid++, connection);
				var gothandshake = false;

				Clients.Add(remoteclient);

				connection.RegisterRawDataHandler("nb3-packet", (packet, _) =>
				{
					var pid = BitConverter.ToInt32(packet.Data[0..4]);
					var data = packet.Data[4..];
					var networkpacket = new NetworkPacket();

					gothandshake = true;

					networkpacket.Data = data;
					networkpacket.Sender = remoteclient;
					networkpacket.Id = pid;

					NetworkPacket.DispatchNetworkPacket(GameManager, networkpacket);
				});

				connection.ConnectionClosed += (reason, _) =>
				{
					if (!remoteclient.IsAboutToLeave)
						LogManager.LogInfo(remoteclient + " is leaving without warning!");
					remoteclient.IsAboutToLeave = true;
					remoteclient.CleanUpRemains();
				};

				Task.Delay(3000).ContinueWith(_ =>
				{
					if (!gothandshake)
					{
						LogManager.LogWarn(connection.IPRemoteEndPoint.Address + " didn't send handshake! disconnecting...");
						connection.Close(CloseReason.NetworkError);
						return;
					}
				});
			};

			Server.Start();
			GameManager.AllowReplication = true;
			GameManager.PhysicsManager.DisablePhysics = false;

			LogManager.LogInfo($"Listening at {Server.IPAddress}:{ServerPort}");

			// but actually we are not done

			while (!GameManager.ShuttingDown)
			{
				while (AppManager.BlockReplication || Clients.Count == 0)
					Thread.Yield();

				if (ReplicationQueue.Count != 0)
				{
					lock (ReplicationQueue)
					{
						var rq = ReplicationQueue.Dequeue();
						var rc = rq.Recievers;
						var ins = rq.Target;

						switch (rq.Mode)
						{
							case Replication.REPM_TOALL:
								rc = [.. Clients];
								break;
							case Replication.REPM_BUTOWNER:
								var cl = Clients.Count;
								rc = new RemoteClient[cl - 1];
								for (int i = 0, j = 0; j < cl - 1; i++, j++)
								{
									if (Clients[i] == ins.Owner)
									{
										i++;
										continue;
									}
									rc[j] = Clients[i];
								}
								break;
							case Replication.REPM_TORECIEVERS:
								break;
						}

						if (rc.Length == 0) continue;

						for (int i = 0; i < rc.Length; i++)
						{
							var nc = rc[i];
							nc.SendPacket(NPReplication.Create(rq));
						}
					}
				}
			}
		}
		public void SendServerboundPacket(NetworkPacket packet)
		{
			ProfileOutgoing(packet.Id, packet.Data);

			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(packet.Id);
			writer.Write(packet.Data);

			RemoteConnection.SendRawData("nb3-packet", stream.ToArray());
		}
		public unsafe void ConnectToServer(IPAddress ipa)
		{
			if (IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			var cn = ConnectionResult.TCPConnectionNotAlive;
			var tcp = ConnectionFactory.CreateTcpConnection(ipa.ToString(), ServerPort, out cn)
				?? throw new Exception("Remote server had refused to connect");

			RemoteConnection = tcp;

			void OnClose(CloseReason cr, Connection c)
			{
				GameManager.RenderManager?.ShowKickMessage("The server had closed (" + cr + ")");
				GameManager.ProhibitScripts = true;
				GameManager.IsRunning = false;
			}

			tcp.EnableLogging = false;
			tcp.KeepAlive = !Debugger.IsAttached;
			tcp.ConnectionClosed += OnClose;

			tcp.RegisterRawDataHandler("nb3-packet", (packet, _) =>
			{
				var pid = BitConverter.ToInt32(packet.Data[0..4]);
				var data = packet.Data[4..];
				var networkpacket = new NetworkPacket();

				networkpacket.Data = data;
				networkpacket.Sender = null;
				networkpacket.Id = pid;

				ProfileIncoming(pid, data);

				NetworkPacket.DispatchNetworkPacket(GameManager, networkpacket);
			});

			NetworkPacket np = NPClientIntroduction.Create(GameManager.Username, new()
			{
				["isguest"] = GameManager.CurrentProfile.IsOffline ? "true" : "false",
				["userid"] = GameManager.CurrentProfile.UserId.ToString()
			});

			SendServerboundPacket(np);

			while (!GameManager.ShuttingDown)
			{
				while (AppManager.BlockReplication) ;

				if (ReplicationQueue.Count != 0)
				{
					lock (ReplicationQueue)
					{
						var rq = ReplicationQueue.Dequeue();
						var ins = rq.Target;

						switch (rq.What)
						{
							case Replication.REPW_PROPCHG:
								SendServerboundPacket(NPReplication.Create(rq));
								break;
						}
					}
				}
			}
		}
		public void StartProfiling()
		{
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
		public void ProfileIncoming(int id, byte[] data)
		{
			var len = 15 + data.Length;
			if (NetworkProfilerLog)
				Debug.WriteLine($"!! nmprofiler, INCOMING #{incomingPacketsRecieved++}, id: {id}, data len: {data.Length}, incoming bytes/sec: {IncomingTraffic} !!");
			incomingTraffic += len;
		}
		public void ProfileOutgoing(int id, byte[] data)
		{
			var len = 15 + data.Length;
			if (NetworkProfilerLog)
				Debug.WriteLine($"!! nmprofiler, OUTGOING #{outgoingPacketsSent++}, id: {id}, data len: {data.Length}, outgoing bytes/sec: {OutgoingTraffic} !!");
			outgoingTraffic += len;
		}
		public void PerformKick(RemoteClient? nc, string msg, bool islocal)
		{
			// it's not really constitutionally defined, but idc.
			if (RemoteConnection == null) return;
			if (IsClient && !islocal)
				throw new ScriptRuntimeException("Cannot kick non-local player from client");
			if (IsClient && islocal)
			{
				RemoteConnection.Close(CloseReason.ClientClosed);
				GameManager.RenderManager?.ShowKickMessage(msg);
				return;
			}

			// we are on server
			if (nc == null) throw new ScriptRuntimeException("RemoteClient object not preserved!");
			nc.KickOut(msg);
		}
		public Replication? AddReplication(Instance inst, int m, int w, bool rc = true, RemoteClient[]? nc = null)
		{
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
		public static string TranslateErrorCode(int ec) => ec switch
		{
			100 => "The server is full",
			101 => "Server only accepts internal connections",
			102 => "Authorization failed",
			103 => "A player with same name is already playing",
			104 => "Server has closed",
			_ => "Unknown connection failure",
		};
	}
}
