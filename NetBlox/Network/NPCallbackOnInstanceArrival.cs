using NetBlox.Instances;

namespace NetBlox.Network
{
	public class NPCallbackOnInstanceArrival : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPCallbackOnInstanceArrival;

		public static NetworkPacket Create(Guid guid)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(guid.ToByteArray());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			// server instructed us to wait for an instance for special treatment.
			// special treatment is me unable to make out a plan to replicate the
			// player's character and assign it as playable in normal way.

			Guid guid = new Guid(reader.ReadBytes(16));
			Instance? maybeExists = gm.GetInstance(guid);

			if (maybeExists != null)
			{
				gm.NetworkManager.SendServerboundPacket(Create(guid));
				return;
			}

			(Guid, Action)? tuple = null;

			tuple = (guid, new Action(() =>
			{
				gm.NetworkManager.AwaitingForArrival.Remove(tuple.Value); // c# go to hell
				gm.NetworkManager.SendServerboundPacket(Create(guid));
			}));

			gm.NetworkManager.AwaitingForArrival.Add(tuple.Value);
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			// we got the instance and we're gonna let the server know.

			Guid guid = new Guid(reader.ReadBytes(16));

			Replication.AwaitingInstanceMap[(packet.Sender, guid)]();
		}
	}
}
