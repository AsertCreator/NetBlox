using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTest
{
	public sealed class NetworkClient : IDisposable
	{
		/*
		public event EventHandler ConnectionEstablished;
		public event EventHandler ConnectionClosed;
		*/
		public TcpClient TcpClient { get; private set; }
		
		public void Dispose()
		{
			TcpClient.Close();
			TcpClient.Dispose();
		}
	}
}
