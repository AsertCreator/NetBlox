using NetBlox.Instances;
using Network;
using System.Text;

namespace NetBlox
{
	public enum RemoteClientConnectionPhase
	{
		AwaitingHandshake, AwaitingPlayer, Replicating
	}
	public class RemoteClientEventArgs : EventArgs 
	{ 
		public RemoteClient RemoteClient { get; set; }
		public NetworkManager NetworkManager { get; set; }
	}
	public class RemoteClientRpcEventArgs : RemoteClientEventArgs
	{
		public RpcMethodInvoke MethodInvoke { get; set; }
	}
	public class RemoteClient(NetworkManager container, Connection connection, int cid)
    {
		public int ClientID = cid;
		public long ClientUserID;
		public string ClientUsername;
		public Player? ClientPlayer;
		public Connection? Connection = connection;
		public NetworkC2SHandshake Handshake;
		public NetworkC2SPlayerDelivery PlayerDelivery;
		public RemoteClientConnectionPhase Phase = RemoteClientConnectionPhase.AwaitingHandshake;

		public event EventHandler<RemoteClientRpcEventArgs> OnRpcCallQueued;
		public event EventHandler<RemoteClientRpcEventArgs> OnRpcCallSent;
		public event EventHandler<RemoteClientEventArgs> OnClientHandshaked;
		public event EventHandler<RemoteClientEventArgs> OnClientDisconnected;

		public Queue<RpcMethodInvoke> ProcessQueue = [];

		private void CheckRPCRequirements()
		{
			if (container.NetworkIdentity != NetworkIdentity.Server || container.NetworkIdentity != NetworkIdentity.ServerStudio)
				throw new InvalidOperationException("Cannot execute RPC calls on client!");
			if (Connection == null || !Connection.IsAlive)
				throw new InvalidOperationException("Cannot execute this RPC call because the target client is disconnected!");
		}
		public void RPC_Kick(string msg)
		{
			CheckRPCRequirements();

			var args = $"{msg}";
			var rmi = new RpcMethodInvoke()
			{
				MethodType = RpcMethodType.Kick,
				MethodArguments = Encoding.UTF8.GetBytes(args)
			};

			ProcessQueue.Enqueue(rmi);
			RaiseOnRpcCallQueued(rmi);
		}

		internal void RaiseOnRpcCallQueued(RpcMethodInvoke rmi) => OnRpcCallQueued?.Invoke(this, new()
		{
			MethodInvoke = rmi,
			NetworkManager = container,
			RemoteClient = this
		});
		internal void RaiseOnRpcCallSent(RpcMethodInvoke rmi) => OnRpcCallSent?.Invoke(this, new()
		{
			MethodInvoke = rmi,
			NetworkManager = container,
			RemoteClient = this
		});
		internal void RaiseOnClientHandshaked() => OnClientHandshaked?.Invoke(this, new()
		{
			NetworkManager = container,
			RemoteClient = this
		});
		internal void RaiseOnClientDisconnected() => OnClientDisconnected?.Invoke(this, new()
		{
			NetworkManager = container,
			RemoteClient = this
		});
	}
}
