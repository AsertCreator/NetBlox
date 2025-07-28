using NetBlox.Instances;
using NetBlox.Instances.Services;

namespace NetBlox.Network
{
	public class NPCharacterReset : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = 7;

		public static NetworkPacket Create(Instance character)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(character.FindFirstChild("Humanoid").UniqueID.ToByteArray());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			Player player = packet.Sender.Player;
			Humanoid humanoid = gm.GetInstance(new Guid(reader.ReadBytes(16))) as Humanoid;
			Humanoid expected = player.Character.FindFirstChild("Humanoid") as Humanoid;

			if (humanoid == expected)
			{
				humanoid.Health = 0;
				humanoid.ReplicateProperties(["Health"], true);
			}
		}
	}
}
