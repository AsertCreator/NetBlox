using NetBlox.Instances;
using NetBlox.Instances.Services;

namespace NetBlox.Network
{
	public class NPStartReplication : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPStartReplication;

		public static NetworkPacket Create(int flags)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(flags);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) 
		{
			var rc = packet.Sender;
			var root = gm.CurrentRoot;
			var netmgr = gm.NetworkManager;
			var toReceivers = Replication.REPM_TORECIEVERS;
			var newInstance = Replication.REPW_NEWINST; // i will not tolerate code that doesn't fit on my 15 inch fhd screen

			gm.NetworkManager.ClientsReadyForReplication.Add(packet.Sender);

			netmgr.AddReplication(root.GetService<ReplicatedFirst>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<ReplicatedStorage>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<Chat>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<Lighting>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<Players>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<Workspace>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<StarterGui>(), toReceivers, newInstance, true, [rc]);
			netmgr.AddReplication(root.GetService<StarterPack>(), toReceivers, newInstance, true, [rc]);
		}
	}
}
