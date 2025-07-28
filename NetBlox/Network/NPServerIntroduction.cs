using NetBlox.Instances;

namespace NetBlox.Network
{
	public class NPServerIntroduction : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = 2;

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
			public Guid DataModelInstance;
			public Guid PlayerInstance;
		}

		public static NetworkPacket Create(GameManager gm, RemoteClient rc, int errcode)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			int instancecount = gm.CurrentRoot.CountReplicatableClientObjects();

			writer.Write(gm.CurrentIdentity.PlaceName);
			writer.Write(gm.CurrentIdentity.UniverseName);
			writer.Write(gm.CurrentIdentity.Author);
			writer.Write(gm.CurrentIdentity.PlaceID);
			writer.Write(gm.CurrentIdentity.UniverseID);
			writer.Write(gm.CurrentIdentity.MaxPlayerCount);
			writer.Write(rc.UniquePlayerID);
			writer.Write(errcode);
			writer.Write(instancecount);
			writer.Write(gm.CurrentRoot.UniqueID.ToByteArray());
			writer.Write(rc.Player.UniqueID.ToByteArray());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) 
		{
			ServerHandshake handshake = new();
			handshake.PlaceName = reader.ReadString();
			handshake.UniverseName = reader.ReadString();
			handshake.Author = reader.ReadString();
			handshake.PlaceID = reader.ReadUInt64();
			handshake.UniverseID = reader.ReadUInt64();
			handshake.MaxPlayerCount = reader.ReadUInt32();
			handshake.UniquePlayerID = reader.ReadUInt32();
			handshake.ErrorCode = reader.ReadInt32();
			handshake.InstanceCount = reader.ReadInt32();
			handshake.DataModelInstance = new Guid(reader.ReadBytes(16));
			handshake.PlayerInstance = new Guid(reader.ReadBytes(16));

			gm.CurrentIdentity.PlaceName = handshake.PlaceName;
			gm.CurrentIdentity.UniverseName = handshake.UniverseName;
			gm.CurrentIdentity.Author = handshake.Author;
			gm.CurrentIdentity.PlaceID = handshake.PlaceID;
			gm.CurrentIdentity.UniverseID = handshake.UniverseID;
			gm.CurrentIdentity.MaxPlayerCount = handshake.MaxPlayerCount;
			gm.CurrentIdentity.UniquePlayerID = handshake.UniquePlayerID;

			gm.CurrentRoot.GetService<CoreGui>().ShowTeleportGui(
				gm.CurrentIdentity.PlaceName, gm.CurrentIdentity.Author,
				(int)gm.CurrentIdentity.PlaceID, (int)gm.CurrentIdentity.UniverseID);
			gm.NetworkManager.IsLoaded = false;

			gm.NetworkManager.ExpectedLocalPlayerGuid = handshake.PlayerInstance;
			gm.CurrentRoot.UniqueID = handshake.DataModelInstance;
			gm.CurrentRoot.Name = gm.CurrentIdentity.PlaceName;

			if (handshake.ErrorCode != 0)
				return;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
