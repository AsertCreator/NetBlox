using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Network
{
	public class NPRemoteEvent : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPRemoteEvent;

		public static NetworkPacket Create(RemoteEvent even, DynValue value)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			byte[] bytes = SerializationManager.SerializeLuaObject(value, even.GameManager);

			writer.Write(even.UniqueID.ToByteArray());
			writer.Write(BitConverter.GetBytes(bytes.Length));
			writer.Write(bytes);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			var remoteevent = gm.GetInstance(new Guid(reader.ReadBytes(16))) as RemoteEvent;
			var eventdatasize = reader.ReadInt32();
			var eventdata = reader.ReadBytes(eventdatasize);
			var dynvalue = SerializationManager.DeserializeLuaObject(eventdata, gm);

			remoteevent.OnClientEvent.Fire(dynvalue);
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			var remoteevent = gm.GetInstance(new Guid(reader.ReadBytes(16))) as RemoteEvent;
			var eventdatasize = reader.ReadInt32();
			var eventdata = reader.ReadBytes(eventdatasize);
			var dynvalue = SerializationManager.DeserializeLuaObject(eventdata, gm);

			remoteevent.OnServerEvent.Fire(LuaRuntime.PushInstance(packet.Sender.Player), dynvalue);
		}
	}
}
