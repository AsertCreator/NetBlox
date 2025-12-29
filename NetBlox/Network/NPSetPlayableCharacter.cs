using NetBlox.Instances;
using NetBlox.Instances.Services;

namespace NetBlox.Network
{
	public class NPSetPlayableCharacter : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPSetPlayableCharacter;

		public static NetworkPacket Create(Model model)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(model.UniqueID.ToByteArray());

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			Model model = gm.GetInstance(new Guid(reader.ReadBytes(16))) as Model;
			Humanoid humanoid = model.FindFirstChild("Humanoid") as Humanoid;

			if (humanoid == null)
				return;

			int reentrancy = 0;

			TaskScheduler.ScheduleJob(JobType.Miscellaneous, _ =>
			{
				Player? localPlayer = gm.CurrentRoot.GetService<Players>().LocalPlayer as Player;

				if (localPlayer == null)
				{
					if (++reentrancy == 100)
					{
						LogManager.LogError("Got a NPSetPlayableCharacter packet but no local Player arrived!");
						return JobResult.CompletedFailure;
					}
					return JobResult.NotCompleted;
				}

				localPlayer.Character = model;
				return JobResult.CompletedSuccess;
			});
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
	}
}
