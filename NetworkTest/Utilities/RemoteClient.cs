using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTest.Utilities
{
	public class RemoteClient(int clientID, TcpClient client, NetworkServer server)
	{
		public int ClientID = clientID;
		public TcpClient Client = client;
		public NetworkServer Server = server;
		public event DataRecievedEventHandler DataRecieved;
		public object? AdditionalObject1;
		public object? AdditionalObject2;
		public object? AdditionalObject3;
		public object? AdditionalObject4;
		private DateTime DoNotPingUntil;

		public void Send(HighPacket hp) => Server.BroadcastToClient(this, hp.PacketID, hp.Payload);
		public void RecieveData(HighPacket hp) => DataRecieved(new(), new(this, Server) { Packet = hp });
		public void SendPing()
		{
			var dn = DateTime.UtcNow;
			if (dn >= DoNotPingUntil)
			{
				Task.Run(() =>
				{
					Send(new HighPacket(1, BitConverter.GetBytes(dn.Ticks)));
					DoNotPingUntil = DateTime.UtcNow.AddMilliseconds(Server.PingInterval);
					while (Client.Available != 4 && DateTime.UtcNow < DoNotPingUntil) ;

					if (DateTime.UtcNow >= DoNotPingUntil)
					{
						Server.DisconnectClient(this);
						return;
					}

					byte[] ack = new byte[4];
					Client.GetStream().Read(ack);
				});
			}
		}
	}
}
