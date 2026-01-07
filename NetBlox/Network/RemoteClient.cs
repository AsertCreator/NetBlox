using NetBlox.Instances;
using Network;
using System.IO;

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

		public Job SendingJob;
		public Job ReceivingJob;

		public Queue<NetworkPacket> PendingSendPackets;
		public Queue<NetworkPacket> PendingProcessPackets;

		public Dictionary<Instance, InstanceReplicationStatus> ReplicatedStatus = [];

		public event EventHandler<Instance>? OnInstanceNewReplicatedTo;
		public event EventHandler<Instance>? OnInstancePropchgReplicatedTo;
		public event EventHandler<Instance>? OnInstancePropchgReplicatedFrom;

		public RemoteClient(GameManager gm, uint uniquePlayerID, Connection connection)
		{
			UniquePlayerID = uniquePlayerID;
			Connection = connection;
			Enclosure = gm;

			PendingSendPackets = [];
			PendingProcessPackets = [];

			SendingJob = TaskScheduler.ScheduleNamedJob("Client-" + uniquePlayerID + "-NetworkSending",
				JobType.Network, NetworkSendingJobHandler, level: 9);
			ReceivingJob = TaskScheduler.ScheduleNamedJob("Client-" + uniquePlayerID + "-NetworkProcessing",
				JobType.Network, NetworkReceivingJobHandler, level: 9);
		}

		private JobResult NetworkSendingJobHandler(Job job)
		{
			if (IsAboutToLeave)
				return JobResult.CompletedSuccess;

			// this is so hacky and prone to breaking i can feel it
			// if it so happens that the client isn't sending anything the job prolly shouldn't get much more cpu time

			SendingJob.JobTimingContext.Priority = 1;

			int batchsize = NetworkManager.ServersideSendingJobPacketBatchSize;

			for (int i = 0; i < batchsize && PendingSendPackets.Count > 0; i++)
			{
				var packet = PendingSendPackets.Dequeue();
				using MemoryStream stream = new();
				using BinaryWriter writer = new(stream);

				writer.Write(packet.Id);
				writer.Write(packet.Data);

				Connection.SendRawData("nb3-packet", stream.ToArray());

				SendingJob.JobTimingContext.Priority = 10;
			}

			return JobResult.NotCompleted;
		}
		private JobResult NetworkReceivingJobHandler(Job job)
		{
			if (IsAboutToLeave)
				return JobResult.CompletedSuccess;

			// ditto

			ReceivingJob.JobTimingContext.Priority = 1;

			int batchsize = NetworkManager.ServersideProcessingJobPacketBatchSize;

			for (int i = 0; i < batchsize && PendingProcessPackets.Count > 0; i++)
			{
				NetworkPacket packet;

				lock (PendingProcessPackets)
					packet = PendingProcessPackets.Dequeue();

				try
				{
					NetworkPacket.DispatchNetworkPacket(Enclosure, packet);
				}
				catch (Exception ex)
				{
					LogManager.LogWarn("RemoteClient-" + UniquePlayerID + ": failed to process packet: " + ex.GetType() +
						", msg: " + ex.Message + ", type: " + packet.Id);
				}

				ReceivingJob.JobTimingContext.Priority = 6;
			}

			return JobResult.NotCompleted;
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

			TaskScheduler.Terminate(SendingJob);
			TaskScheduler.Terminate(ReceivingJob);
		}
		public void KickOut(string message)
		{
			NetworkPacket networkPacket = NPClientDisconnection.Create(message);
			SendPacket(networkPacket);
			IsAboutToLeave = true;
		}
		public void SendPacket(NetworkPacket packet) => PendingSendPackets.Enqueue(packet);
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
