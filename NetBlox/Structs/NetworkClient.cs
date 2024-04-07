using NetBlox.Instances;
using Network;

namespace NetBlox.Structs
{
	public class NetworkClient
	{
		public string? Username;
		public uint UniquePlayerID;
		public Connection? Connection;
		public Player? Player;
		public bool IsDisconnecting;
	}
}
