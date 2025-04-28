using NetBlox.Instances;
using Network.Packets;
using Network;
using System.Net;
using CloseReason = Network.Enums.CloseReason;
using NetBlox.Instances.Services;

namespace NetBlox
{
	public class NetworkClient(NetworkManager nm)
	{
		public RemoteClient? SelfRemoteClient;
		public Connection? RemoteConnection;
		public bool IsLoaded => AwaitingInstanceCount >= GotNewInstances;

		private Guid AwaitingDataModel;
		private Guid AwaitingPlayer;
		private int AwaitingInstanceCount;
		private decimal GotNewInstances;

		public GameManager GameManager => nm.GameManager;
		public DataModel Root => GameManager.CurrentRoot;

		public void ForcefullyDisconnect()
		{
			LogManager.LogWarn("Forcefully disconnecting from the server...");
			RemoteConnection.Close(CloseReason.ClientClosed);
		}

		private void ProcessServerHandshake(RawData rawdata)
		{
			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms))
			{
				var networkS2CHandshake = new NetworkS2CHandshake();

				networkS2CHandshake.ProtocolVersion = br.ReadInt64();
				networkS2CHandshake.PlaceName = br.ReadString();
				networkS2CHandshake.UniverseName = br.ReadString();
				networkS2CHandshake.PlaceId = br.ReadInt64();
				networkS2CHandshake.UniverseId = br.ReadInt64();
				networkS2CHandshake.AuthorUsername = br.ReadString();
				networkS2CHandshake.AuthorId = br.ReadInt64();
				networkS2CHandshake.RemoteClientId = br.ReadInt64();
				networkS2CHandshake.RemoteClientCount = br.ReadInt64();
				networkS2CHandshake.MaxPlayerCount = br.ReadInt64();

				GameManager.PlaceIdentity.PlaceName = networkS2CHandshake.PlaceName;
				GameManager.PlaceIdentity.UniverseName = networkS2CHandshake.UniverseName;
				GameManager.PlaceIdentity.PlaceID = (ulong)networkS2CHandshake.PlaceId;
				GameManager.PlaceIdentity.UniverseID = (ulong)networkS2CHandshake.UniverseId;
				GameManager.PlaceIdentity.UniquePlayerID = (uint)networkS2CHandshake.RemoteClientId;
				GameManager.PlaceIdentity.Author = networkS2CHandshake.AuthorUsername;
				GameManager.PlaceIdentity.MaxPlayerCount = (uint)networkS2CHandshake.MaxPlayerCount;

				Root.Name = GameManager.PlaceIdentity.PlaceName;

				Root.GetService<CoreGui>().ShowTeleportGui(GameManager.PlaceIdentity.PlaceName, GameManager.PlaceIdentity.Author,
					(int)GameManager.PlaceIdentity.PlaceID, (int)GameManager.PlaceIdentity.UniverseID);
			}

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				var networkC2SPlayerDelivery = new NetworkC2SPlayerDelivery()
				{
					NetBloxUserAgent = "NetBlox",
					OnMobileDevice = GameManager.CurrentProfile.IsMouseDevice,
					OnTouchEnabledDevice = GameManager.CurrentProfile.IsTouchDevice,
					LocalTime = DateTime.Now,
					IsScriptingEnabled = true,
				};

				bw.Write(networkC2SPlayerDelivery.NetBloxUserAgent);
				bw.Write(networkC2SPlayerDelivery.LocalTime.Ticks);
				bw.Write(networkC2SPlayerDelivery.OnMobileDevice);
				bw.Write(networkC2SPlayerDelivery.OnTouchEnabledDevice);
				bw.Write(networkC2SPlayerDelivery.IsScriptingEnabled);

				RemoteConnection.SendRawData("NB3READY", ms.ToArray());
				SelfRemoteClient.Phase = RemoteClientConnectionPhase.AwaitingPlayer;
			}
		}
		private void ProcessServerPlayerDelivery(RawData rawdata)
		{
			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms))
			{
				var networkS2CPlayerDelivery = new NetworkS2CPlayerDelivery();

				networkS2CPlayerDelivery.DataModelGuid = new Guid(br.ReadBytes(16));
				networkS2CPlayerDelivery.LocalPlayerGuid = new Guid(br.ReadBytes(16));
				networkS2CPlayerDelivery.EstimatedInstanceCount = br.ReadInt32();

				AwaitingDataModel = networkS2CPlayerDelivery.DataModelGuid;
				AwaitingPlayer = networkS2CPlayerDelivery.LocalPlayerGuid;
				AwaitingInstanceCount = networkS2CPlayerDelivery.EstimatedInstanceCount;
			}

			Root.UniqueID = AwaitingDataModel;
			GameManager.NetworkManager.SpawnReplication();
			SelfRemoteClient.Phase = RemoteClientConnectionPhase.Replicating;
		}
		private void ProcessServerReplication(RawData rawdata)
		{
			using (var ms = new MemoryStream(rawdata.Data))
			using (var br = new BinaryReader(ms))
			{
				var header = br.ReadByte();

				if (header == 0xF0) // complete
				{
					var data = rawdata.Data[1..];
					var instance = NetworkSerializer.DeserializeInstanceComplete(data, GameManager);

					if (instance.UniqueID == AwaitingPlayer)
					{
						var player = (Player)instance;
						player.IsLocalPlayer = true;
						((Players)(instance.Parent)).CurrentPlayer = player;
					}

					if (++GotNewInstances == (int)(AwaitingInstanceCount * 0.75f)) // TODO: oh my god
						Root.GetService<CoreGui>().HideTeleportGui();
				}
				else if (header == 0xF1) // delta
				{
					var data = rawdata.Data[1..];
					NetworkSerializer.ApplyInstanceDelta(data, GameManager);
				}
				else if (header == 0xFF) // destroy
				{
					var guid = new Guid(br.ReadBytes(16));
					var instance = GameManager.GetInstance(guid);
					instance?.Destroy();
				}
			}
		}
		private void OnReplicationPacketReceived(RawData rawdata, Connection connection)
		{
			if (SelfRemoteClient.Phase == RemoteClientConnectionPhase.Replicating)
				ProcessServerReplication(rawdata);
			else
			{
				LogManager.LogWarn("Out of order replication packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnect();
			}
		}
		private void OnHandshakePacketReceived(RawData rawdata, Connection connection)
		{
			if (SelfRemoteClient.Phase == RemoteClientConnectionPhase.AwaitingHandshake)
				ProcessServerHandshake(rawdata);
			else
			{
				LogManager.LogWarn("Out of order handshake packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnect();
			}
		}
		private void OnPlayerDeliveryPacketReceived(RawData rawdata, Connection connection)
		{
			if (SelfRemoteClient.Phase == RemoteClientConnectionPhase.AwaitingPlayer)
				ProcessServerPlayerDelivery(rawdata);
			else
			{
				LogManager.LogWarn("Out of order player delivery packet! (" + connection.IPRemoteEndPoint + ")");
				ForcefullyDisconnect();
			}
		}
		private void OnConnectionClosed(CloseReason reason, Connection connection)
		{
			ForcefullyDisconnect();
		}

		public void InitializeNetworkClient(IPAddress ipa, int port)
		{
			ConnectionResult result;
			RemoteConnection = ConnectionFactory.CreateTcpConnection(ipa.ToString(), port, out result);

			if (result != ConnectionResult.Connected)
				throw new Exception("Could not connect to the remote server!");

			RemoteConnection.KeepAlive = false;
			RemoteConnection.ConnectionClosed += OnConnectionClosed;
			RemoteConnection.RegisterRawDataHandler("NB3REPL", OnReplicationPacketReceived);
			RemoteConnection.RegisterRawDataHandler("NB3HDSHK", OnHandshakePacketReceived);
			RemoteConnection.RegisterRawDataHandler("NB3READY", OnPlayerDeliveryPacketReceived);

			var remoteClient = new RemoteClient(nm, RemoteConnection, -1);
			SelfRemoteClient = remoteClient;
			SelfRemoteClient.ClientUserID = GameManager.CurrentProfile.UserId;
			SelfRemoteClient.ClientUsername = GameManager.CurrentProfile.Username;
			SelfRemoteClient.Phase = RemoteClientConnectionPhase.AwaitingHandshake;

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				var networkC2SHandshake = new NetworkC2SHandshake();
				networkC2SHandshake.ProtocolVersion = NetworkManager.ProtocolVersion;
				networkC2SHandshake.UserId = SelfRemoteClient.ClientUserID;
				networkC2SHandshake.Username = SelfRemoteClient.ClientUsername;
				networkC2SHandshake.UserAuthChallengeAnswer = "";
				networkC2SHandshake.UserAppearanceId = SelfRemoteClient.ClientUserID;

				bw.Write(networkC2SHandshake.ProtocolVersion);
				bw.Write(networkC2SHandshake.UserId);
				bw.Write(networkC2SHandshake.Username);
				bw.Write(networkC2SHandshake.UserAuthChallengeAnswer);
				bw.Write(networkC2SHandshake.UserAppearanceId);

				SelfRemoteClient.Handshake = networkC2SHandshake;
				SelfRemoteClient.Phase = RemoteClientConnectionPhase.AwaitingHandshake;
				RemoteConnection.SendRawData("NB3HDSHK", ms.ToArray());
			}
		}
		public void NetworkStep()
		{
			// TODO: uhhh
		}
	}
}
