using NetBlox.Instances;
using Network;

namespace NetBlox.Network
{
	public enum InstanceReplicationStatus
	{
		NotReplicatedYet, Replicated, Updated
	}

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

		public Dictionary<Instance, InstanceReplicationStatus> ReplicatedStatus = [];

		public event EventHandler<Instance>? OnInstanceNewReplicatedTo;
		public event EventHandler<Instance>? OnInstancePropchgReplicatedTo;
		public event EventHandler<Instance>? OnInstancePropchgReplicatedFrom;

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

			Enclosure.NetworkManager.Clients.Remove(this);
			Enclosure.NetworkManager.ClientsReadyForReplication.Remove(this);
			Player?.Destroy();
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
			Enclosure.NetworkManager.awaitingForRemoteArrival.Add(new()
			{
				Client = this,
				Guid = inst.UniqueID,
				Callback = callback
			});
			SendPacket(NPCallbackOnInstanceArrival.Create(inst.UniqueID));
		}
		public void NotifyNewReplicatedTo(Instance inst)
		{

		}
		public void NotifyPropchgReplicatedTo(Instance inst)
		{

		}
		public void NotifyPropchgReplicatedFrom(Instance inst)
		{

		}
		public override string ToString()
		{
			if (Username == null)
				return Connection.IPRemoteEndPoint.ToString();
			return Username;
		}
	}
}
