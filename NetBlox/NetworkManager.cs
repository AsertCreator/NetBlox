using NetBlox.Instances;
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

			StartServerNonBlocking(ipa, port);
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
	}
}
