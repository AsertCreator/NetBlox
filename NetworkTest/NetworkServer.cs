using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NetworkTest.Utilities;

namespace NetworkTest
{
	public sealed class NetworkServer : IDisposable
	{
		public List<RemoteClient> Clients = [];
		public event ClientToServerEventHandler OnClientConnected;
		public event ClientToServerEventHandler OnClientDisconnected;
		public int PingInterval = 5000;
		private Queue<HighPacket> SendQueue = [];
		private TcpListener Listener;
		private int NextClientId = 0;
		private bool ShuttingDown = false;

		public NetworkServer()
		{
			Listener = new(IPAddress.Any, 60000);
			Listener.Start();
		}
		public void Dispose()
		{
			ShuttingDown = true;
			Listener.Stop();
			Listener.Dispose();
		}
		public void BroadcastToAll(ushort packetid, byte[] payload) =>
			SendQueue.Enqueue(new(packetid, payload, HighPacketMode.BroadcastToAll));
		public void BroadcastToClient(RemoteClient nc, ushort packetid, byte[] payload) =>
			SendQueue.Enqueue(new(packetid, payload, HighPacketMode.BroadcastToSpecific, [nc.ClientID]));
		public void BroadcastToClients(RemoteClient[] ncs, ushort packetid, byte[] payload) =>
			SendQueue.Enqueue(new(packetid, payload, HighPacketMode.BroadcastToSpecific, ncs.Select(x => x.ClientID).ToArray()));
		public void DisconnectClient(RemoteClient nc)
		{
			lock (Clients)
			{
				OnClientDisconnected(this, new(nc));
				Clients.Remove(nc);
				nc.Client.Close();
				nc.Client.Dispose();
			}
		}
		public void Start()
		{
			Listener.BeginAcceptTcpClient(ProcessClient, null);
			Task.Run(() =>
			{
				while (!ShuttingDown)
				{
					while (Clients.Count == 0 || SendQueue.Count == 0) ;
					HighPacket hp = SendQueue.Dequeue();
					byte[] bytes = SetupPacket(hp.PacketID, hp.Payload);

					switch (hp.Mode)
					{
						case HighPacketMode.BroadcastToAll:
							{
								RemoteClient[] cls = Clients.ToArray();
								for (int i = 0; i < cls.Length; i++)
								{
									RemoteClient nc = cls[i];
									nc.Client.GetStream().Write(bytes);
								}
								break;
							}
						case HighPacketMode.BroadcastToSpecific:
							{
								RemoteClient[] cls = Clients.ToArray();
								for (int i = 0; i < cls.Length; i++)
								{
									RemoteClient nc = cls[i];
									if (hp.Recievers.Contains(nc.ClientID))
										nc.Client.GetStream().Write(bytes);
								}
								break;
							}
						default: // just discard it
							break;
					}

					for (int i = 0; i < Clients.Count; i++)
					{
						RemoteClient nc = Clients[i];
						NetworkStream ns = nc.Client.GetStream();
						if (nc.Client.Available >= 4)
						{
							byte[] header = new byte[4];
							ns.Read(header, 0, 4);
							ushort paylen = BitConverter.ToUInt16(header.AsSpan()[2..4]);
							byte[] payload = new byte[paylen];
							ns.Read(header, 0, paylen);

							HighPacket packet = new HighPacket();
							packet.PacketID = BitConverter.ToUInt16(header.AsSpan()[0..2]);
							packet.Payload = payload;

							nc.RecieveData(packet);
							nc.SendPing();
						}
					}
				}
			});
		}
		private void ProcessClient(IAsyncResult res)
		{
			while (!res.IsCompleted) ;
			TcpClient tcpClient = Listener.EndAcceptTcpClient(res);

			try
			{
				lock (Clients)
				{
					RemoteClient nc = new(NextClientId++, tcpClient, this);
					byte[] rawack = new byte[4];

					tcpClient.ReceiveTimeout = 1000;
					tcpClient.GetStream().Write(SetupPacket(1, BitConverter.GetBytes(nc.ClientID)));
					if (tcpClient.GetStream().Read(rawack, 0, 4) == 0)
						return;

					Clients.Add(nc);
					OnClientConnected(this, new(nc));
				}
			}
			finally
			{
				Listener.BeginAcceptTcpClient(ProcessClient, null);
			}
		}
		private byte[] SetupPacket(ushort packetid, byte[] payload)
		{
			if (payload.Length > ushort.MaxValue)
				throw new NetworkException("Too big network payload!");

			byte[] data =
			[
				.. BitConverter.GetBytes(packetid),
					.. BitConverter.GetBytes((ushort)payload.Length),
					.. payload,
				];
			return data;
		}
	}
}
