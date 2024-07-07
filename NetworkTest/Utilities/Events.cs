using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTest.Utilities
{
	public class ClientToServerEventArgs(RemoteClient nc) : EventArgs
	{
		public RemoteClient Client = nc;
		public NetworkServer Server => Client.Server;
	}
	public delegate void ClientToServerEventHandler(object sender, ClientToServerEventArgs e);

	public class DataRecievedEventArgs(RemoteClient? nc, NetworkServer? ns) : EventArgs
	{
		public RemoteClient? Client = nc;
		public NetworkServer? Server = ns;
		public HighPacket Packet;
	}
	public delegate void DataRecievedEventHandler(object sender, DataRecievedEventArgs e);
	public class NetworkException(string msg) : Exception(msg);
}
