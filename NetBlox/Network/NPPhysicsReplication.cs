using NetBlox.Instances;
using System.Numerics;

namespace NetBlox.Network
{
	public class NPPhysicsReplication : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPPhysicsReplication;

		public static NetworkPacket Create(BasePart part)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(part.UniqueID.ToByteArray());
			writer.Write(part._position.X);
			writer.Write(part._position.Y);
			writer.Write(part._position.Z);
			writer.Write(part._rotation.X);
			writer.Write(part._rotation.Y);
			writer.Write(part._rotation.Z);
			writer.Write(part._rotation.W);
			writer.Write(part.LinearVelocity.X);
			writer.Write(part.LinearVelocity.Y);
			writer.Write(part.LinearVelocity.Z);
			writer.Write(part.RotationalVelocity.X);
			writer.Write(part.RotationalVelocity.Y);
			writer.Write(part.RotationalVelocity.Z);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			BasePart basepart = gm.GetInstance(new Guid(reader.ReadBytes(16))) as BasePart;
			Vector3 position = default;
			Quaternion rotation = default;
			Vector3 linear = default;
			Vector3 angular = default;

			position.X = reader.ReadSingle();
			position.Y = reader.ReadSingle();
			position.Z = reader.ReadSingle();
			rotation.X = reader.ReadSingle();
			rotation.Y = reader.ReadSingle();
			rotation.Z = reader.ReadSingle();
			rotation.W = reader.ReadSingle();
			linear.X = reader.ReadSingle();
			linear.Y = reader.ReadSingle();
			linear.Z = reader.ReadSingle();
			angular.X = reader.ReadSingle();
			angular.Y = reader.ReadSingle();
			angular.Z = reader.ReadSingle();

			basepart._physicsposition = position;
			basepart._physicsrotation = rotation;
			basepart._physicsvelocity = linear;
			basepart.AngularVelocity = angular;
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			Player player = packet.Sender.Player;
			BasePart basepart = gm.GetInstance(new Guid(reader.ReadBytes(16))) as BasePart;
			Vector3 position = default;
			Quaternion rotation = default;
			Vector3 linear = default;
			Vector3 angular = default;

			if (basepart.Owner != player.Client)
			{
				LogManager.LogWarn(player.Client + " tried to replicate physics for a part that they don't own!");
				return;
			}

			position.X = reader.ReadSingle();
			position.Y = reader.ReadSingle();
			position.Z = reader.ReadSingle();
			rotation.X = reader.ReadSingle();
			rotation.Y = reader.ReadSingle();
			rotation.Z = reader.ReadSingle();
			rotation.W = reader.ReadSingle();
			linear.X = reader.ReadSingle();
			linear.Y = reader.ReadSingle();
			linear.Z = reader.ReadSingle();
			angular.X = reader.ReadSingle();
			angular.Y = reader.ReadSingle();
			angular.Z = reader.ReadSingle();

			basepart._physicsposition = position;
			basepart._physicsrotation = rotation;
			basepart._physicsvelocity = linear;
			basepart.AngularVelocity = angular;
		}
	}
}
