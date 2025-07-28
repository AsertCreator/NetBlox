using NetBlox.Instances;
using NetBlox.Instances.Services;

namespace NetBlox.Network
{
	public class NPChat : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = 6;

		public static NetworkPacket Create(ChatMessage message)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(message.Sender != null);
			if (message.Sender != null)
				writer.Write(message.Sender.UniqueID.ToByteArray());
			writer.Write(message.Message);

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			ChatMessage chatMessage = new ChatMessage();
			chatMessage.Sender = gm.GetInstance(new Guid(reader.ReadBytes(16))) as Player;
			chatMessage.Message = reader.ReadString();

			gm.CurrentRoot.GetService<Chat>().ProcessMessage(chatMessage);
		}
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			ChatMessage chatMessage = new ChatMessage();
			chatMessage.Sender = gm.GetInstance(new Guid(reader.ReadBytes(16))) as Player;
			chatMessage.Message = reader.ReadString();

			if (chatMessage.Sender == null || chatMessage.Sender.Client != packet.Sender)
			{
				var name = chatMessage.Sender == null ? "system messages" : chatMessage.Sender.Name;
				LogManager.LogWarn(packet.Sender + " tried to impersonate " + name + " in chat!");
				return;
			}

			gm.CurrentRoot.GetService<Chat>().ProcessMessage(chatMessage);
		}
	}
}
