using NetworkTest.Utilities;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkTest
{
	internal static class Program
	{
		internal static void Main(string[] args)
		{
			Console.Write("Press S key to switch to server mode: ");
			if (Console.ReadKey(true).Key == ConsoleKey.S) RunServer();
			else RunClient();
		}
		internal static void RunServer()
		{
			NetworkServer ns = new();
			ns.OnClientConnected += (x, e) =>
			{
				e.Client.Send(new HighPacket(100, Encoding.UTF8.GetBytes("NetworkTest message!")));
			};
			ns.Start();
		}
		internal static void RunClient()
		{

		}
	}
}
