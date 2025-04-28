using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using System.Net;

namespace NetBlox
{
	public enum NetworkIdentity
	{
		Client, Server, ClientStudio, ServerStudio
	}
	public struct NetworkC2SPlayerDelivery
	{
		public string NetBloxUserAgent;
		public DateTime LocalTime;
		public bool OnMobileDevice;
		public bool OnTouchEnabledDevice;
		public bool IsScriptingEnabled;
	}
	public struct NetworkS2CPlayerDelivery
	{
		public Guid DataModelGuid;
		public Guid LocalPlayerGuid;
		public int EstimatedInstanceCount;
	}
	public struct NetworkC2SHandshake
	{
		public long ProtocolVersion;
		public long UserId;
		public string Username;
		public string UserAuthChallengeAnswer;
		public long UserAppearanceId;
	}
	public struct NetworkS2CHandshake
	{
		public long ProtocolVersion;
		public string PlaceName;
		public string UniverseName;
		public long PlaceId;
		public long UniverseId;
		public string AuthorUsername;
		public long AuthorId;
		public long RemoteClientId;
		public long RemoteClientCount;
		public long MaxPlayerCount;
	}
	public sealed class NetworkManager
	{
		public GameManager GameManager;
		public NetworkIdentity NetworkIdentity;
		public NetworkClient? NetworkClient;
		public NetworkServer? NetworkServer;
		public const int ProtocolVersion = 1;
		public bool PolicyBroadcastedServer = true;
		public Task? ServerProcessingTask;
		public Job ReplicationJob;
		public Queue<Replication> ReplicationQueue = [];
		public DataModel Root => GameManager.CurrentRoot;
		public bool IsClientGame => (int)NetworkIdentity % 2 == 0;
		public bool IsServerGame => (int)NetworkIdentity % 2 != 0;

		public NetworkManager(GameManager gm, NetworkIdentity identity)
		{
			GameManager = gm;
			NetworkIdentity = identity;
		}
		public void StartServerNonBlocking()
		{
			var ipa = PolicyBroadcastedServer ? IPAddress.Any : IPAddress.Loopback;
			var port = Random.Shared.Next(30000, 35000);

			var forceport = AppManager.FastInts["FIntForceServerPort"];
			if (forceport != -1)
				port = forceport;

			StartServerNonBlocking(ipa, port);
			SpawnReplication();
		}
		public void StartServerNonBlocking(IPAddress ipa, int port)
		{
			NetworkServer = new(this);
			ServerProcessingTask = NetworkServer.InitializeNetworkServer(ipa, port);

			LogManager.LogWarn("NetworkServer had started on " + ipa + ", port: " + port);
		}
		public void StartClientNonBlocking(IPAddress ipa, int port)
		{
			NetworkClient = new NetworkClient(this);
			NetworkClient.InitializeNetworkClient(ipa, port);

			LogManager.LogWarn("Connecting to " + ipa + ", port: " + port + "...");
		}
		public void ReplicateInstance(Instance inst, RemoteClient nc, bool includechildren)
		{
			ReplicationQueue.Enqueue(new() { Instance = inst, BroadcastType = ReplicationBroadcastType.Target, 
				Type = ReplicationType.NewInstance, Target = nc });
			if (includechildren)
			{
				var desc = inst.GetDescendants();
				for (int i = 0; i < desc.Length; i++)
					ReplicationQueue.Enqueue(new() { Instance = inst, BroadcastType = ReplicationBroadcastType.Target, 
						Type = ReplicationType.NewInstance, Target = nc });
			}
		}
		public void ReplicateInstanceToAll(Instance inst)
		{
			ReplicationQueue.Enqueue(new() { Instance = inst, BroadcastType = ReplicationBroadcastType.All, 
				Type = ReplicationType.NewInstance, Target = null });
		}
		public void ReplicateInstanceToAllButOwner(Instance inst)
		{
			ReplicationQueue.Enqueue(new() { Instance = inst, BroadcastType = ReplicationBroadcastType.AllButTarget, 
				Type = ReplicationType.NewInstance, Target = inst.Owner });
		}
		public void SpawnReplication()
		{
			ReplicationJob = TaskScheduler.ScheduleJob(JobType.Replication, x =>
			{
				for (int i = 0; i < 50 && ReplicationQueue.Count > 0; i++)
				{
					var replication = ReplicationQueue.Dequeue();
					var packet = replication.GeneratePacket();

					if (replication.Instance == null)
					{
						i--;
						continue;
					}
					if (replication.Instance is Script)
					{
						i--;
						break;
					}

					if (IsClientGame)
						NetworkClient.RemoteConnection.SendRawData("NB3REPL", packet);
					else
					{
						List<RemoteClient> clients = [];

						switch (replication.BroadcastType)
						{
							case ReplicationBroadcastType.Target:
								clients.Add(replication.Target);
								break;
							case ReplicationBroadcastType.AllButTarget:
								clients.AddRange(NetworkServer.AllClients);
								clients.Remove(replication.Target);
								break;
							case ReplicationBroadcastType.All:
								clients.AddRange(NetworkServer.AllClients);
								break;
						}

						clients.ForEach(x => x.Connection.SendRawData("NB3REPL", packet));
					}
				}

				if (!GameManager.ShuttingDown)
					return JobResult.NotCompleted;
				return JobResult.CompletedSuccess;
			}, level: 9);
			ReplicationJob.ScriptJobContext.GameManager = GameManager;
		}
	}
}
