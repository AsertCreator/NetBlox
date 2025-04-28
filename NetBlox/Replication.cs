using NetBlox.Instances;
using System.Reflection;

namespace NetBlox
{
	public enum ReplicationBroadcastType
	{
		Target, AllButTarget, All
	}
	public enum ReplicationType
	{
		NewInstance, InstanceDelta, Destroy
	}
	public class Replication
	{
		public RemoteClient? Target;
		public ReplicationBroadcastType BroadcastType;
		public ReplicationType Type;
		public Instance? Instance;
		public List<string>? DeltaProperties;

		public byte[] GeneratePacket()
		{
			using MemoryStream ms = new();
			using BinaryWriter bw = new(ms);

			switch (Type)
			{
				case ReplicationType.NewInstance:
					var arr0 = NetworkSerializer.SerializeInstanceComplete(Instance);
					bw.Write((byte)0xF0);
					bw.Write(arr0);
					break;
				case ReplicationType.InstanceDelta:
					PropertyInfo[] info = new PropertyInfo[DeltaProperties.Count];
					for (int i = 0;	i < DeltaProperties.Count; i++)
						info[i] = Instance.GetType().GetProperty(DeltaProperties[i]);
					var delta = NetworkSerializer.SerializeInstanceDelta(Instance, info);
					bw.Write((byte)0xF1);
					bw.Write(delta);
					break;
				case ReplicationType.Destroy:
					bw.Write((byte)0xFF);
					bw.Write(Instance.UniqueID.ToByteArray());
					break;
			}

			return ms.ToArray();
		}
	}
}
