using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Callables;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Network
{
	public class NPRemoteFunction : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPRemoteFunction;

		public static NetworkPacket Create(RemoteFunction function, DynValue value, bool isReturning, Guid callId, 
			string? errorMessage, bool isErroring)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			byte[] bytes = SerializationManager.SerializeLuaObject(value, function.GameManager);

			writer.Write(function.UniqueID.ToByteArray());
			writer.Write(isReturning);
			writer.Write(isErroring);
			if (isErroring)
				writer.Write(errorMessage);
			writer.Write(callId.ToByteArray());
			writer.Write(BitConverter.GetBytes(bytes.Length));
			writer.Write(bytes);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			var remoteFunction = gm.GetInstance(new Guid(reader.ReadBytes(16))) as RemoteFunction;
			var isReturning = reader.ReadBoolean();
			var isErroring = reader.ReadBoolean();
			string? errorMessage = null;
			if (isErroring)
				errorMessage = reader.ReadString();
			var callId = new Guid(reader.ReadBytes(16));
			var eventdatasize = reader.ReadInt32();
			var eventdata = reader.ReadBytes(eventdatasize);
			var dynvalue = SerializationManager.DeserializeLuaObject(eventdata, gm);

			if (isReturning)
				remoteFunction.InternalHandleCallEnd(dynvalue, callId, errorMessage);
			else
				remoteFunction.InternalHandleCallStart(dynvalue, callId, errorMessage);
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			var remoteFunction = gm.GetInstance(new Guid(reader.ReadBytes(16))) as RemoteFunction;
			var isReturning = reader.ReadBoolean();
			var isErroring = reader.ReadBoolean();
			string? errorMessage = null;
			if (isErroring)
				errorMessage = reader.ReadString();
			var callId = new Guid(reader.ReadBytes(16));
			var eventdatasize = reader.ReadInt32();
			var eventdata = reader.ReadBytes(eventdatasize);
			var dynvalue = SerializationManager.DeserializeLuaObject(eventdata, gm);

			if (isReturning)
				remoteFunction.InternalHandleCallEnd(dynvalue, callId, errorMessage);
			else
				remoteFunction.InternalHandleCallStart(dynvalue, callId, errorMessage);
		}
	}
}
