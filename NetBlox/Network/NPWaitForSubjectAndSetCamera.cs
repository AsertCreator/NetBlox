using NetBlox.Instances;

namespace NetBlox.Network
{
	public class NPWaitForSubjectAndSetCamera : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPWaitForSubjectAndSetCamera;

		public static NetworkPacket Create(Instance subject, DateTime expiration)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(subject.UniqueID.ToByteArray());
			writer.Write(expiration.ToBinary());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) 
		{ 
			Guid awaitedInstance = new Guid(packet.Data[0..16]);
			DateTime awaitedInstanceExpiration = new DateTime(BitConverter.ToInt64(packet.Data[16..24]));

			gm.NetworkManager.WaitForInstanceArrival(awaitedInstance, () =>
			{
				if (DateTime.UtcNow > awaitedInstanceExpiration)
					return;

				Instance received = gm.GetInstance(awaitedInstance);

				gm.RenderManager.CurrentCamera.CameraSubject = received;
			});
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
