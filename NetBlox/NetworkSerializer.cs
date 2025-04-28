using NetBlox.Instances;
using NetBlox.Runtime;
using System.Reflection;

namespace NetBlox
{
	public static class NetworkSerializer
	{
		public static byte[] SerializeInstanceDelta(Instance inst, PropertyInfo[] props)
		{
			var type = inst.GetType();
			var gm = inst.GameManager;

			using (MemoryStream ms = new())
			using (BinaryWriter bw = new(ms))
			{
				bw.Write(inst.UniqueID.ToByteArray());
				bw.Write(inst.ParentID.ToByteArray());

				var pos = ms.Position;

				bw.Write((short)0);

				int acti = 0;

				for (int i = 0; i < props.Length; i++)
				{
					var prop = props[i];
					var propType = prop.PropertyType;
					var propValue = prop.GetValue(inst);

					if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
						continue;

					var serialized = SerializationManager.NetworkSerializers[propType.FullName](propValue, gm);

					bw.Write(prop.MetadataToken);
					bw.Write((short)serialized.Length);
					bw.Write(serialized);

					acti++;
				}

				ms.Position = pos;
				bw.Write((short)acti);

				return ms.ToArray();
			}
		}
		public static void ApplyInstanceDelta(byte[] delta, GameManager gm)
		{
			using (MemoryStream ms = new(delta))
			using (BinaryReader br = new(ms))
			{
				var uniqueId = new Guid(br.ReadBytes(16));
				var parentId = new Guid(br.ReadBytes(16));
				var inst = gm.GetInstance(uniqueId) ?? throw new InvalidDataException("Incorrect instance delta!");
				var type = inst.GetType();

				if (inst.ParentID != parentId)
				{
					var newParent = gm.GetInstance(parentId);
					if (newParent == null)
						throw new InvalidDataException("Can't set instance's parent to a non-existing instance");
					inst.Parent = newParent;
				}

				int acti = br.ReadInt16();
				var props = type.GetProperties();

				for (int i = 0; i < acti; i++)
				{
					var pid = br.ReadInt32();
					var prop = Array.Find(props, (x) => x.MetadataToken == pid);
					var propType = prop.PropertyType;
					var serializedLength = br.ReadInt16();
					var serialized = br.ReadBytes(serializedLength);
					var deserialized = SerializationManager.NetworkDeserializers[propType.FullName](serialized, gm);

					prop.SetValue(inst, deserialized);
				}
			}
		}
		public static byte[] SerializeInstanceComplete(Instance inst)
		{
			var type = inst.GetType();
			var gm = inst.GameManager;
			var props = type.GetProperties();

			using (MemoryStream ms = new())
			using (BinaryWriter bw = new(ms))
			{
				bw.Write(inst.ClassName);
				bw.Write(inst.UniqueID.ToByteArray());
				bw.Write(inst.ParentID.ToByteArray());

				var pos = ms.Position;

				bw.Write((short)0);

				int acti = 0;

				for (int i = 0; i < props.Length; i++)
				{
					var prop = props[i];
					var propType = prop.PropertyType;
					var propValue = prop.GetValue(inst);

					if (prop.GetCustomAttribute<NotReplicatedAttribute>() != null)
						continue;
					if (prop.SetMethod == null)
						continue;
					if (propValue is Guid || propValue is LuaSignal)
						continue;

					if (SerializationManager.NetworkSerializers.TryGetValue(propType.FullName, out var ser))
					{
						var serialized = ser(propValue, gm);

						bw.Write(prop.GetMetadataToken());
						bw.Write((short)serialized.Length);
						bw.Write(serialized);

						acti++;
					}
				}

				ms.Position = pos;
				bw.Write((short)acti);

				return ms.ToArray();
			}
		}
		public static Instance DeserializeInstanceComplete(byte[] full, GameManager gm)
		{
			Instance instance;

			using (MemoryStream ms = new(full))
			using (BinaryReader br = new(ms))
			{
				instance = InstanceCreator.CreateReplicatedInstance(br.ReadString(), gm);
				var uniqueId = new Guid(br.ReadBytes(16));
				var parentId = new Guid(br.ReadBytes(16));
				var type = instance.GetType();

				instance.UniqueID = uniqueId;
				instance.Parent = gm.GetInstance(parentId);
				if (instance.Parent == null && parentId != default)
					throw new InvalidDataException("Can't set instance's parent to a non-existing instance");
				instance.ParentID = parentId;

				int acti = br.ReadInt16();
				var props = type.GetProperties();

				for (int i = 0; i < acti; i++)
				{
					Security.Impersonate(8);
					var token = br.ReadInt32();
					var prop = Array.Find(props, x => x.GetMetadataToken() == token);
					var propType = prop.PropertyType;
					var serializedLength = br.ReadInt16();
					var serialized = br.ReadBytes(serializedLength);
					var deserialized = SerializationManager.NetworkDeserializers[propType.FullName](serialized, gm);

					prop.SetValue(instance, deserialized);
					Security.EndImpersonate();
				}
			}

			return instance;
		}
	}
}
