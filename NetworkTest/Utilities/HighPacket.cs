using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTest.Utilities
{
	public struct HighPacket(ushort packetID, byte[] payload, HighPacketMode mode = HighPacketMode.BroadcastToAll, int[]? recievers = null)
	{
		public HighPacketMode Mode = mode;
		public ushort PacketID = packetID;
		public byte[] Payload = payload;
		public int[] Recievers = recievers ?? [];
	}
	public enum HighPacketMode : ushort
	{
		BroadcastToAll, BroadcastToSpecific
	}
}
