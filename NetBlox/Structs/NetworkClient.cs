using NetBlox.Instances;
using Network;

namespace NetBlox.Structs
{
	/// <summary>
	/// Represents a client from server's POV
	/// </summary>
	public class NetworkClient
	{
		public string? Username;
		public uint UniquePlayerID;
		public Connection? Connection;
		public Player? Player;
		public bool IsDisconnecting;
	}
}
