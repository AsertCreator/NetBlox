namespace NetBlox.Network
{
	public class NPClientDisconnection : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPClientDisconnection;

		private struct ClientDisconnection
		{
			public ushort ServerVersionMajor;
			public ushort ServerVersionMinor;
			public ushort ServerVersionPatch;
			public string KickMessage;
			public bool IsSystemMessage;
		}

		// i wish c# had "static method contracts" so i could make these statics standardized.

		public static NetworkPacket Create(string message, bool isSystemMessage = false)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write((ushort)Common.Version.VersionMajor);
			writer.Write((ushort)Common.Version.VersionMinor);
			writer.Write((ushort)Common.Version.VersionPatch);
			writer.Write(message);
			writer.Write(isSystemMessage);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) 
		{
			ClientDisconnection disconnection = new ClientDisconnection();
			disconnection.ServerVersionMajor = reader.ReadUInt16();
			disconnection.ServerVersionMinor = reader.ReadUInt16();
			disconnection.ServerVersionPatch = reader.ReadUInt16();
			disconnection.KickMessage = reader.ReadString();
			disconnection.IsSystemMessage = reader.ReadBoolean();

			packet.Sender.Connection.Close(global::Network.Enums.CloseReason.ClientClosed);
			gm.RenderManager?.ShowKickMessage(disconnection.KickMessage, disconnection.IsSystemMessage);
			gm.ProhibitScripts = true;
			gm.IsRunning = false;

			return;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
