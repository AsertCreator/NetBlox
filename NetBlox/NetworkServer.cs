using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Network;
using Network.Enums;
using Network.Packets;
using System.Net;
using CloseReason = Network.Enums.CloseReason;

namespace NetBlox
{
	public class NetworkServer(NetworkManager nm)
	{
		public List<RemoteClient> AllClients = [];
		public ServerConnectionContainer? ConnectionContainer;
		private int NextClientId = 0;

		public GameManager GameManager => nm.GameManager;
		public DataModel Root => GameManager.CurrentRoot;

		public void SendAllRpcCalls()
		{
			for (int i = 0; i < AllClients.Count; i++)
			{
				using MemoryStream ms = new();
				var remoteClient = AllClients[i];
				var connection = remoteClient.Connection;
				var queue = remoteClient.ProcessQueue;

				while (queue.TryDequeue(out RpcMethodInvoke rmi))
				{
					using BinaryWriter bw = new(ms);

					bw.Write((int)rmi.MethodType);
					bw.Write(rmi.MethodArguments);

					connection.SendRawData("NB3RPC", ms.ToArray());
					ms.Position = 0;
					ms.SetLength(0);

					remoteClient.RaiseOnRpcCallSent(rmi);
				}
			}
		}

		public void PerformServersideCleanup(RemoteClient rc)
		{
			AllClients.Remove(rc);

			rc.RaiseOnClientDisconnected();
			rc.ClientPlayer.Client = null;
			rc.ClientPlayer.Destroy();

			LogManager.LogWarn(rc.ClientUsername + " disconnected!");
		}
		public void ForcefullyDisconnectRemoteClient(RemoteClient rc)
		{
			rc.Connection.Close(CloseReason.ServerClosed);
			PerformServersideCleanup(rc);
		}

		private void ProcessRemoteClientHandshake(RawData rawdata, Connection connection, RemoteClient rc)
		{
			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms)) 
			{
				var networkC2SHandshake = new NetworkC2SHandshake();
				// im scared that csc will change the order of these.
				networkC2SHandshake.ProtocolVersion = br.ReadInt64();
				networkC2SHandshake.UserId = br.ReadInt64();
				networkC2SHandshake.Username = br.ReadString();
				networkC2SHandshake.UserAuthChallengeAnswer = br.ReadString();
				networkC2SHandshake.UserAppearanceId = br.ReadInt64();

				if (networkC2SHandshake.UserAuthChallengeAnswer == "__MANUALFAIL")
				{
					LogManager.LogWarn("Client failed to pass the authentication challenge (" + connection.IPRemoteEndPoint + ")"); // ashame them
					ForcefullyDisconnectRemoteClient(rc);
					return;
				}
				if (networkC2SHandshake.ProtocolVersion != NetworkManager.ProtocolVersion)
				{
					LogManager.LogWarn("Client is on the different version than the server.");
					ForcefullyDisconnectRemoteClient(rc);
					return;
				}

				rc.ClientUserID = networkC2SHandshake.UserId;
				rc.ClientUsername = networkC2SHandshake.Username;
				rc.Handshake = networkC2SHandshake;
			}

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				var networkS2CHandshake = new NetworkS2CHandshake
				{
					ProtocolVersion = NetworkManager.ProtocolVersion,
					PlaceName = GameManager.PlaceIdentity.PlaceName,
					UniverseName = GameManager.PlaceIdentity.UniverseName,
					RemoteClientId = rc.ClientID,
					RemoteClientCount = AllClients.Count,
					AuthorId = -1, // TODO: this
					AuthorUsername = GameManager.PlaceIdentity.Author,
					PlaceId = (long)GameManager.PlaceIdentity.PlaceID,
					UniverseId = (long)GameManager.PlaceIdentity.UniverseID
				};

				bw.Write(networkS2CHandshake.ProtocolVersion);
				bw.Write(networkS2CHandshake.PlaceName);
				bw.Write(networkS2CHandshake.UniverseName);
				bw.Write(networkS2CHandshake.PlaceId);
				bw.Write(networkS2CHandshake.UniverseId);
				bw.Write(networkS2CHandshake.AuthorUsername);
				bw.Write(networkS2CHandshake.AuthorId);
				bw.Write(networkS2CHandshake.RemoteClientId);
				bw.Write(networkS2CHandshake.RemoteClientCount);
				bw.Write(networkS2CHandshake.MaxPlayerCount);

				connection.SendRawData("NB3HDSHK", ms.ToArray());
				rc.Phase = RemoteClientConnectionPhase.AwaitingPlayer;
			}
		}
		private void ProcessRemoteClientPlayerDelivery(RawData rawdata, Connection connection, RemoteClient rc)
		{
			Player player;

			Security.Impersonate(8);

			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms))
			{
				var networkC2SPlayerDelivery = new NetworkC2SPlayerDelivery();
				// im scared that csc will change the order of these.
				networkC2SPlayerDelivery.NetBloxUserAgent = br.ReadString();
				networkC2SPlayerDelivery.LocalTime = new DateTime(br.ReadInt64());
				networkC2SPlayerDelivery.OnMobileDevice = br.ReadBoolean();
				networkC2SPlayerDelivery.OnTouchEnabledDevice = br.ReadBoolean();
				networkC2SPlayerDelivery.IsScriptingEnabled = br.ReadBoolean();

				player = new(GameManager);
				player.Name = rc.ClientUsername;
				player.Parent = Root.GetService<Players>();
				player.Client = rc;
				player.IsLocalPlayer = false;

				// TODO: fetch data from public service

				player.SetAccountAge(-1);
				player.SetUserId(rc.ClientUserID);
				player.CharacterAppearanceId = rc.Handshake.UserAppearanceId;

				rc.PlayerDelivery = networkC2SPlayerDelivery;
				rc.RaiseOnClientHandshaked();
			}

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				var networkS2CPlayerDelivery = new NetworkS2CPlayerDelivery
				{
					DataModelGuid = Root.UniqueID,
					LocalPlayerGuid = player.UniqueID,
					EstimatedInstanceCount = GameManager.AllInstances.Count - 5 // TODO: normal estimating
				};

				bw.Write(networkS2CPlayerDelivery.DataModelGuid.ToByteArray());
				bw.Write(networkS2CPlayerDelivery.LocalPlayerGuid.ToByteArray());
				bw.Write(networkS2CPlayerDelivery.EstimatedInstanceCount);

				connection.SendRawData("NB3READY", ms.ToArray());
				rc.Phase = RemoteClientConnectionPhase.Replicating;
			}

			nm.ReplicateInstance(Root.GetService<ReplicatedFirst>(), rc, true);
			nm.ReplicateInstance(Root.GetService<Players>(), rc, true);
			nm.ReplicateInstance(Root.GetService<Workspace>(), rc, true);
			nm.ReplicateInstance(Root.GetService<ReplicatedStorage>(), rc, true);
			nm.ReplicateInstance(Root.GetService<Lighting>(), rc, true);

			Security.EndImpersonate();
		}
		private void ProcessRemoteClientReplication(RawData rawdata, Connection connection, RemoteClient rc)
		{
			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms))
			{
				// TODO: replication
			}

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				// TODO: replication
			}
		}
		private void OnReplicationPacketReceived(RawData rawdata, Connection connection)
		{
			var rc = GetRemoteClientByConnection(connection);
			if (rc == null)
				return;

			if (rc.Phase == RemoteClientConnectionPhase.Replicating)
				ProcessRemoteClientReplication(rawdata, connection, rc);
			else
			{
				LogManager.LogWarn("Out of order replication packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnectRemoteClient(rc);
			}
		}
		private void OnHandshakePacketReceived(RawData rawdata, Connection connection)
		{
			var rc = GetRemoteClientByConnection(connection);
			if (rc == null)
				return;

			if (rc.Phase == RemoteClientConnectionPhase.AwaitingHandshake)
				ProcessRemoteClientHandshake(rawdata, connection, rc);
			else
			{
				LogManager.LogWarn("Out of order handshake packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnectRemoteClient(rc);
			}
		}
		private void OnPlayerDeliveryPacketReceived(RawData rawdata, Connection connection)
		{
			var rc = GetRemoteClientByConnection(connection);
			if (rc == null)
				return;

			if (rc.Phase == RemoteClientConnectionPhase.AwaitingPlayer)
				ProcessRemoteClientPlayerDelivery(rawdata, connection, rc);
			else
			{
				LogManager.LogWarn("Out of order player delivery packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnectRemoteClient(rc);
			}
		}
		private void OnConnectionClosed(CloseReason reason, Connection connection)
		{
			var rc = GetRemoteClientByConnection(connection);
			if (rc == null)
				return;
			PerformServersideCleanup(rc);
		}
		private void OnConnectionEstablished(Connection connection, ConnectionType type)
		{
			var remoteClient = new RemoteClient(nm, connection, NextClientId++);
			connection.KeepAlive = false;
			connection.ConnectionClosed += OnConnectionClosed;
			connection.RegisterRawDataHandler("NB3REPL", OnReplicationPacketReceived);
			connection.RegisterRawDataHandler("NB3HDSHK", OnHandshakePacketReceived);
			connection.RegisterRawDataHandler("NB3READY", OnPlayerDeliveryPacketReceived);
			AllClients.Add(remoteClient);
		}
		private RemoteClient? GetRemoteClientByConnection(Connection connection)
		{
			for (int i = 0; i < AllClients.Count; i++)
			{
				var client = AllClients[i];
				if (client.Connection == connection)
					return client;
			}
			return null;
		}

		public Task InitializeNetworkServer(IPAddress ipa, int port)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (ipa.Address != 0) // lol as if i care
				ConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(ipa.ToString(), port, false);
			else
				ConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(port, false);
#pragma warning restore CS0618 // Type or member is obsolete
			ConnectionContainer.ConnectionEstablished += OnConnectionEstablished;
			return ConnectionContainer.StartTCPListener();
		}
		public void NetworkStep()
		{
			SendAllRpcCalls();
		}
	}
}
