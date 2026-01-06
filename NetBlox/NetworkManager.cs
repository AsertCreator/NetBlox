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
using System.Threading.Tasks;

namespace NetBlox
{
	public sealed class NetworkManager
	{
		public readonly static Type InstanceType = typeof(Instance);
		public readonly static Type LST = typeof(LuaSignal);

		public GameManager GameManager;
		public List<RemoteClient> Clients = [];
		public HashSet<RemoteClient> ClientsReadyForReplication = [];
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

		public List<Replication> ReplicationQueue = [];

		public Connection? RemoteConnection;
		public ServerConnectionContainer? Server;
		public CancellationTokenSource ClientReplicatorCanceller = new();
		public Task<object>? ClientReplicator;

		public Job? ReplicationJob;

		public int LoadedInstanceCount;
		public int TargetInstanceCount;
		public bool LogReplication = false;

		internal int outgoingPacketsSent = 0;
		internal int incomingPacketsRecieved = 0;
		internal int outgoingTraffic = 0;
		internal int incomingTraffic = 0;

		internal List<NetworkAwaiter> awaitingForArrival = [];
		internal List<RemoteNetworkAwaiter> awaitingForRemoteArrival = [];
		internal uint nextpid = 0;
		internal bool init;

		internal class NetworkAwaiter
		{
			public Guid Guid;
			public Action Callback;
		}
		internal class RemoteNetworkAwaiter
		{
			public RemoteClient Client;
			public Guid Guid;
			public Action Callback;
		}

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
		/// Starts NetBlox server on current <seealso cref="NetworkManager"/>, this function is NOT blocking, start it in THE server thread.
		/// </summary>
		public void StartServerNonBlocking()
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
			GameManager.PauseReplication = false;
			GameManager.PhysicsManager.DisablePhysics = false;

			LogManager.LogInfo($"Listening at {Server.IPAddress}:{ServerPort}");

			// but actually we are not done

			ReplicationJob = TaskScheduler.ScheduleNamedJob("MasterReplicator", JobType.Network, _ =>
			{
				if (GameManager.ShuttingDown)
					return JobResult.CompletedSuccess;

				if (AppManager.BlockReplication || GameManager.PauseReplication || ClientsReadyForReplication.Count == 0)
					return JobResult.NotCompleted;

				if (ReplicationQueue.Count != 0)
				{
					var rq = ReplicationQueue[0];

					ReplicationQueue.RemoveAt(0);

					var rc = rq.Recievers;
					var ins = rq.Target;

					if (ins is not BasePart)
						rq.Mode = Replication.REPM_TOALL;

					switch (rq.Mode)
					{
						case Replication.REPM_TOALL:
							rc = [.. ClientsReadyForReplication];
							break;
						case Replication.REPM_BUTOWNER:
							var cl = ClientsReadyForReplication.Count;
							var bp = ins as BasePart;
							var newReceivers = new List<RemoteClient>();

							newReceivers.AddRange(ClientsReadyForReplication);
							newReceivers.Remove(bp.Owner);

							rc = newReceivers.ToArray();

							break;
						case Replication.REPM_TORECIEVERS:
							break;
					}

					if (rc == null || rc.Length == 0)
						return JobResult.NotCompleted;

					for (int i = 0; i < rc.Length; i++)
					{
						var nc = rc[i];
						nc.SendPacket(NPReplication.Create(rq));

						if (LogReplication)
							LogManager.LogInfo("Replicating object to client (" + nc.UniquePlayerID + "): " + rq.Target.GetFullName());
					}	
				}

				return JobResult.NotCompleted;
			}, level: 9);
			ReplicationJob.JobTimingContext.Priority = 30;
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
		public void ConnectToServer(IPAddress ipa)
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

			ReplicationJob = TaskScheduler.ScheduleNamedJob("MasterReplicator", JobType.Network, _ =>
			{
				if (GameManager.ShuttingDown)
					return JobResult.CompletedSuccess;

				if (AppManager.BlockReplication || GameManager.PauseReplication)
					return JobResult.NotCompleted;

				if (ReplicationQueue.Count != 0)
				{
					var rq = ReplicationQueue[0];

					ReplicationQueue.RemoveAt(0);

					var ins = rq.Target;

					switch (rq.What)
					{
						case Replication.REPW_PROPCHG:
							SendServerboundPacket(NPReplication.Create(rq));
							break;
					}
				}

				return JobResult.NotCompleted;
			}, level: 9);
			ReplicationJob.JobTimingContext.Priority = 30;
		}
		public void StartProfiling()
		{
			Task.Run(async () =>
			{
				while (!GameManager.ShuttingDown)
				{
					await Task.Delay(1000);
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
		public int CountPendingNewinstReplicationsFor(RemoteClient client)
		{
			int count = 0;

			for (int i = 0; i < ReplicationQueue.Count; i++)
			{
				var r = ReplicationQueue[i];

				if (r.What != Replication.REPW_NEWINST)
					continue;

				if (r.Mode == Replication.REPM_TOALL)
					count += 1;
				else if (r.Mode == Replication.REPM_TORECIEVERS && r.Recievers.Contains(client))
					count += 1;
			}

			return count;
		}
		public void PerformKick(RemoteClient? nc, string msg, bool islocal)
		{
			if (RemoteConnection == null) return;
			if (IsClient && !islocal)
				throw new ScriptRuntimeException("Cannot kick non-local player from client");

			nc.Player.WasKicked = true;

			if (IsClient && islocal)
			{
				RemoteConnection.Close(CloseReason.ClientClosed);
				GameManager.RenderManager?.ShowKickMessage(msg);
				return;
			}

			// we are on server
			if (nc == null) 
				throw new ScriptRuntimeException("RemoteClient object not preserved!");

			nc.KickOut(msg);
		}
		public void WaitForInstanceArrival(Guid guid, Action act)
		{
			lock (awaitingForArrival)
			{
				Instance inst = GameManager.GetInstance(guid);
				if (inst != null)
					act();

				NetworkAwaiter awaiter = new();
				awaiter.Guid = guid;
				awaiter.Callback = act;
				awaitingForArrival.Add(awaiter);
			}
		}
		public void CallAllInstanceRemoteAwaiters(Guid guid, RemoteClient client)
		{
			lock (awaitingForRemoteArrival)
			{
				for (int i = 0; i < awaitingForRemoteArrival.Count; i++)
				{
					var awaiter = awaitingForRemoteArrival[i];
					if (awaiter.Guid == guid && awaiter.Client == client)
					{
						awaitingForRemoteArrival.Remove(awaiter);
						awaiter.Callback();
						i--;
					}
				}
			}
		}
		public void CallAllInstanceAwaiters(Guid guid)
		{
			lock (awaitingForArrival)
			{
				for (int i = 0; i < awaitingForArrival.Count; i++)
				{
					var awaiter = awaitingForArrival[i];
					if (awaiter.Guid == guid)
					{
						awaitingForArrival.Remove(awaiter);
						awaiter.Callback();
						i--;
					}
				}
			}
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

			if (!inst.EligibleForReplication)
				return null;
			if (ClientsReadyForReplication.Count == 0) 
				return null;

			if (m == Replication.REPM_TORECIEVERS) 
			{
				if (nc == null)
					return null;

				List<RemoteClient> clients = nc.ToList();

				for (int i = 0; i < nc.Length; i++)
				{
					if (!ClientsReadyForReplication.Contains(nc[i]))
					{
						clients.Remove(nc[i]);
					}
				}

				nc = clients.ToArray();
			}

			if (m == Replication.REPM_TORECIEVERS && nc.Length == 0)
				return null;

			var rep = new Replication(m, w, inst)
			{
				Recievers = nc ?? []
			};

			ReplicationQueue.Add(rep);

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
