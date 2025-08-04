using NetBlox.Instances;
using System.Numerics;

namespace NetBlox.Network
{
	public class NPUpdatePlayerBufferZone : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPUpdatePlayerBufferZone;

		private struct UpdatePlayerBufferZone
		{
			public Vector3 Center;
			public int Radius;
		}

		public static NetworkPacket Create(Vector3 center, int radius)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(center.X);
			writer.Write(center.Y);
			writer.Write(center.Z);
			writer.Write(radius);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			Vector3 v3 = new Vector3();
			v3.X = reader.ReadSingle();
			v3.Y = reader.ReadSingle();
			v3.Z = reader.ReadSingle();

			UpdatePlayerBufferZone updatePlayerBufferZone = new UpdatePlayerBufferZone();
			updatePlayerBufferZone.Center = v3;
			updatePlayerBufferZone.Radius = reader.ReadInt32();

			gm.NetworkManager.LocalBufferZoneLimits = updatePlayerBufferZone.Radius;
			gm.NetworkManager.LocalBufferZoneCenter = updatePlayerBufferZone.Center;

			return;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
