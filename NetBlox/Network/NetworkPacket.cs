using System.Reflection;

namespace NetBlox.Network
{
	public struct NetworkPacket
	{
		public static Dictionary<int, NetworkPacketHandler> AllPacketHandlers = [];

		public int Id;
		public byte[] Data;
		public RemoteClient? Sender;

		public NetworkPacket(int id, byte[] data, RemoteClient? sender)
		{
			Id = id;
			Data = data;
			Sender = sender;
		}
		static NetworkPacket()
		{
			var it = typeof(NetworkPacketHandler);
			var handlertypes = (from x in Assembly.GetExecutingAssembly().GetTypes() where x.IsAssignableTo(it) && x != it select x).ToArray();
			var handlers = (from x in handlertypes select x.GetConstructor([]).Invoke(null) as NetworkPacketHandler).ToArray();
			
			for (int i = 0; i < handlers.Length; i++)
			{
				var handler = handlers[i];
				AllPacketHandlers[handler.ProbeTargetPacketId] = handler;
			}
		}
		public unsafe static void DispatchNetworkPacket(GameManager gm, NetworkPacket packet)
		{
			if (AllPacketHandlers.TryGetValue(packet.Id, out var handler))
			{
				using MemoryStream stream = new(packet.Data);
				using BinaryReader reader = new(stream);

				if (packet.Sender != null)
					handler.HandleServerbound(gm, packet, reader);
				else
					handler.HandleClientbound(gm, packet, reader);
			}
			else
				throw new InvalidOperationException("No packet handler is registered for " + packet.Id + "!");
		}
	}
}
