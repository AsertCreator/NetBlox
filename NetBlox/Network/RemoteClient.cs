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
		public GameManager GameManager;
		public bool IsAboutToLeave;
		public int BufferZoneLimits;

		public Job SendingJob;
		public Job ReceivingJob;

		public Queue<NetworkPacket> PendingSendPackets;
		public Queue<NetworkPacket> PendingProcessPackets;

		public RemoteClient(GameManager gm, uint uniquePlayerID, Connection connection)
		{
			UniquePlayerID = uniquePlayerID;
			Connection = connection;
			GameManager = gm;

			PendingSendPackets = [];
			PendingProcessPackets = [];

			SendingJob = TaskScheduler.ScheduleNamedJob("Client-" + uniquePlayerID + "-NetworkSending",
				JobType.Network, NetworkSendingJobHandler, level: 9);
			SendingJob.ScriptJobContext.GameManager = GameManager;
			ReceivingJob = TaskScheduler.ScheduleNamedJob("Client-" + uniquePlayerID + "-NetworkProcessing",
				JobType.Network, NetworkReceivingJobHandler, level: 9);
			ReceivingJob.ScriptJobContext.GameManager = GameManager;
		}

		private JobResult NetworkSendingJobHandler(Job job)
		{
			// this is so hacky and prone to breaking i can feel it
			// if it so happens that the client isn't sending anything the job prolly shouldn't get much more cpu time

			int batchsize = NetworkManager.ServersideSendingJobPacketBatchSize;

			for (int i = 0; i < batchsize && PendingSendPackets.Count > 0; i++)
			{
				var packet = PendingSendPackets.Dequeue();
				using MemoryStream stream = new();
				using BinaryWriter writer = new(stream);

				writer.Write(packet.Id);
				writer.Write(packet.Data);

				Connection.SendRawData("nb3-packet", stream.ToArray());
			}

			if (IsAboutToLeave)
			{
				Connection.Close(global::Network.Enums.CloseReason.ClientClosed);
				GameManager.NetworkManager.ClientsReadyForReplication.Remove(this);
				GameManager.NetworkManager.Clients.Remove(this);
				return JobResult.CompletedSuccess;
			}

			return JobResult.NotCompleted;
		}
		private JobResult NetworkReceivingJobHandler(Job job)
		{
			// ditto

			int batchsize = NetworkManager.ServersideProcessingJobPacketBatchSize;

			for (int i = 0; i < batchsize && PendingProcessPackets.Count > 0; i++)
			{
				NetworkPacket packet;

				lock (PendingProcessPackets)
					packet = PendingProcessPackets.Dequeue();

				try
				{
					NetworkPacket.DispatchNetworkPacket(GameManager, packet);
				}
				catch (Exception ex)
				{
					LogManager.LogWarn("RemoteClient-" + UniquePlayerID + ": failed to process packet: " + ex.GetType() +
						", msg: " + ex.Message + ", type: " + packet.Id);
				}
			}

			if (IsAboutToLeave)
				return JobResult.CompletedSuccess;

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

			GameManager.NetworkManager.Clients.Remove(this);
			GameManager.NetworkManager.ClientsReadyForReplication.Remove(this);
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
			GameManager.NetworkManager.awaitingForRemoteArrival.Add(new()
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
