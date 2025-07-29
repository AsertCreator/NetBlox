namespace NetBlox.Network
{
	public class NPReplication : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPReplication;

		// i wish c# had "static method contracts" so i could make these statics standardized.

		public static NetworkPacket Create(Replication repl)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(repl.Mode);
			writer.Write(repl.What);
			writer.Write(repl.Serialize());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			int mode = reader.ReadInt32();
			int what = reader.ReadInt32();
			byte[] data = packet.Data[8..];

			gm.IsRunning = true;

			Replication.ApplyFromBytes(gm, packet.Sender, mode, what, data);
			return;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			int mode = reader.ReadInt32();
			int what = reader.ReadInt32();
			byte[] data = packet.Data[8..];

			Replication.ApplyFromBytes(gm, packet.Sender, mode, what, data);
			return;
		}
	}
}
