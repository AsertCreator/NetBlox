namespace NetBlox.Network
{
	public abstract class NetworkPacketHandler
	{
		public abstract int ProbeTargetPacketId { get; }

		public abstract void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader);
		public abstract void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader);
	}
}
