using NetBlox.Instances;
using Network;

namespace NetBlox
{
	public class RemoteClient
	{
		public string? Username;
		public uint UniquePlayerID;
		public Connection? Connection;
		public Player? Player;
		public bool IsDisconnecting;
	}
}
