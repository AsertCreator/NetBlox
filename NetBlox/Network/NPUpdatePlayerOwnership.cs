using NetBlox.Instances;

namespace NetBlox.Network
{
	public class NPUpdatePlayerOwnership : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = 5;

		private struct UpdatePlayerOwnership
		{
			public Guid Instance;
			public bool Status;
		}

		public static NetworkPacket Create(Instance inst, bool status)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(inst.UniqueID.ToByteArray());
			writer.Write(status);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			UpdatePlayerOwnership updatePlayerOwnership = new UpdatePlayerOwnership();
			updatePlayerOwnership.Instance = new Guid(reader.ReadBytes(16));
			updatePlayerOwnership.Status = reader.ReadBoolean();

			Instance inst = gm.GetInstance(updatePlayerOwnership.Instance);
			inst.IsDomestic = updatePlayerOwnership.Status;

			inst.OnNetworkOwnershipChanged();

			return;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
