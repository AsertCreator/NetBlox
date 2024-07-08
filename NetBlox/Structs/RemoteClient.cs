using NetBlox.Instances;
using Network;

namespace NetBlox.Structs
{
	/// <summary>
	/// Represents a client from server's POV
	/// </summary>
	public class RemoteClient
	{
		public string Username;
		public uint UniquePlayerID;
		public Connection Connection;
		public Player Player;

		public RemoteClient(string user, uint uniquePlayerID, Connection connection, Player player)
		{
			Username = user;
			UniquePlayerID = uniquePlayerID;
			Connection = connection;
			Player = player;
		}
	}
}
