using MoonSharp.Interpreter;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances.Callables
{
	[Creatable]
	public class RemoteFunction : Instance
	{
		[Lua([Security.Capability.None])]
		public DynValue? OnClientInvoke 
		{
			get
			{
				if (!GameManager.NetworkManager.IsClient)
					throw new InvalidOperationException("Cannot get OnClientInvoke on servers!");
				return onClientInvoke;
			}
			set
			{
				if (!GameManager.NetworkManager.IsClient)
					throw new InvalidOperationException("Cannot set OnClientInvoke on servers!");
				if (value.Type != DataType.Function)
					throw new InvalidOperationException("Cannot set OnClientInvoke to non-function values!");
				onClientInvoke = value;
			}
		}
		[Lua([Security.Capability.None])]
		public DynValue? OnServerInvoke
		{
			get
			{
				if (!GameManager.NetworkManager.IsServer)
					throw new InvalidOperationException("Cannot get OnServerInvoke on servers!");
				return onServerInvoke;
			}
			set
			{
				if (!GameManager.NetworkManager.IsServer)
					throw new InvalidOperationException("Cannot set OnServerInvoke on servers!");
				if (value.Type != DataType.Function)
					throw new InvalidOperationException("Cannot set OnServerInvoke to non-function values!");
				onServerInvoke = value;
			}
		}

		private Dictionary<Guid, RemoteFunctionCallItem> AwaitingItems = [];
		private DynValue? onClientInvoke;
		private DynValue? onServerInvoke;

		public RemoteFunction(GameManager ins) : base(ins) { }

		private class RemoteFunctionCallItem
		{
			public Guid CallId;
			public bool IsIncoming;
			public DynValue Arguments;
			public DynValue? Returned;
			public string? ErrorMessage;
		}

		[Lua([Security.Capability.None])]
		public LuaYield InvokeClient(Player player, [TupleArgument] DynValue args)
		{
			if (GameManager.NetworkManager.IsClient)
				throw new InvalidOperationException("Cannot call RemoteFunction's InvokeClient on clients!");
			if (player == null)
				throw new ArgumentNullException("Player cannot be nil!");

			var item = new RemoteFunctionCallItem();
			item.CallId = Guid.NewGuid();
			item.Arguments = args;
			item.Returned = null;
			item.IsIncoming = false;

			AwaitingItems[item.CallId] = item;

			var networkPacket = NPRemoteFunction.Create(this, args, false, item.CallId, null, false);

			player.Client.SendPacket(networkPacket);

			var currentJob = TaskScheduler.CurrentJob;
			TaskScheduler.CurrentJob.JobTimingContext.JoinedTo = TaskScheduler.ScheduleNamedJob("RemoteFunction-CR-Waiting",
				JobType.Network, x =>
				{
					if (item.ErrorMessage != null)
					{
						// TODO: implement proper error handling for RemoteFunctions
					}
					if (item.Returned != null)
					{
						currentJob.ScriptJobContext.YieldReturn = [item.Returned];
						return JobResult.CompletedSuccess;
					}
					return JobResult.NotCompleted;
				});

			return new LuaYield();
		}
		[Lua([Security.Capability.None])]
		public LuaYield InvokeServer(Player player, [TupleArgument] DynValue args)
		{
			if (GameManager.NetworkManager.IsClient)
				throw new InvalidOperationException("Cannot call RemoteFunction's InvokeClient on clients!");
			if (player == null)
				throw new ArgumentNullException("Player cannot be nil!");

			var item = new RemoteFunctionCallItem();
			item.CallId = Guid.NewGuid();
			item.Arguments = args;
			item.Returned = null;
			item.IsIncoming = false;

			AwaitingItems[item.CallId] = item;

			var networkPacket = NPRemoteFunction.Create(this, args, false, item.CallId, null, false);

			GameManager.NetworkManager.SendServerboundPacket(networkPacket);

			var currentJob = TaskScheduler.CurrentJob;
			TaskScheduler.CurrentJob.JobTimingContext.JoinedTo = TaskScheduler.ScheduleNamedJob("RemoteFunction-SR-Waiting",
				JobType.Network, x =>
				{
					if (item.ErrorMessage != null)
					{
						// TODO: implement proper error handling for RemoteFunctions
					}
					if (item.Returned != null)
					{
						currentJob.ScriptJobContext.YieldReturn = [item.Returned];
						return JobResult.CompletedSuccess;
					}
					return JobResult.NotCompleted;
				});

			return new LuaYield();
		}
		public void InternalHandleCallEnd(DynValue value, Guid callId, string? errorMessage)
		{
			if (AwaitingItems.TryGetValue(callId, out var item))
			{
				AwaitingItems.Remove(callId);
				item.Returned = value;
				item.ErrorMessage = errorMessage;
			}
			else
			{
				LogManager.LogWarn("Unknown InternalHandleCallEnd operation with id " + callId + " sent to " + GetFullName() + ", who is that?");
			}
		}
		public void InternalHandleCallStart(DynValue value, Guid callId, string? errorMessage)
		{
			if (GameManager.NetworkManager.IsClient)
			{
				TaskScheduler.ScheduleScript(GameManager, onClientInvoke, 2, null, job =>
				{
					DynValue returned = job.ScriptJobContext.YieldAnswer;

					var networkPacket = NPRemoteFunction.Create(this, returned, true, callId, null, false);

					GameManager.NetworkManager.SendServerboundPacket(networkPacket);

					return JobResult.CompletedSuccess;
				}, 
				[value]);
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RemoteFunction) == classname) return true;
			return base.IsA(classname);
		}
	}
}
