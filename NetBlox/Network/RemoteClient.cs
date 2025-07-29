using NetBlox.Instances;
using Network;

namespace NetBlox.Network
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
		public GameManager Enclosure;
		public bool IsAboutToLeave;
		public int BufferZoneLimits;

		public RemoteClient(GameManager gm, uint uniquePlayerID, Connection connection)
		{
			UniquePlayerID = uniquePlayerID;
			Connection = connection;
			Enclosure = gm;
		}
		public void SetIdentity(Player player)
		{
			Username = player.Name;
			Player = player;
		}
		public void CleanUpRemains()
		{
			if (!IsAboutToLeave) return;
		}
		public void KickOut(string message)
		{
			NetworkPacket networkPacket = NPClientDisconnection.Create(message);
			SendPacket(networkPacket);
			IsAboutToLeave = true;
		}
		public void SendPacket(NetworkPacket packet)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(packet.Id);
			writer.Write(packet.Data);

			Connection.SendRawData("nb3-packet", stream.ToArray());
		}
		public void	WaitForInstanceArrival(Instance inst, Action callback)
		{
			SendPacket(NPCallbackOnInstanceArrival.Create(inst.UniqueID));
			Replication.AwaitingInstanceMap[(this, inst.UniqueID)] = () =>
			{
				Replication.AwaitingInstanceMap.Remove((this, inst.UniqueID));
				callback();
			};
		}
		public override string ToString()
		{
			if (Username == null)
				return Connection.IPRemoteEndPoint.ToString();
			return Username;
		}
	}
}
