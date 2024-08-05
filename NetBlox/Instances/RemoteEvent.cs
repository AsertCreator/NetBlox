using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class RemoteEvent : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal OnClientEvent { get; set; } = new();
		[Lua([Security.Capability.None])]
		public LuaSignal OnServerEvent { get; set; } = new();
		public Action<object[]>? FastCallback;

		public RemoteEvent(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public void FireServer(DynValue table)
		{
			if (!GameManager.NetworkManager.IsClient)
				throw new Exception("Cannot call FireServer on server!");

			GameManager.NetworkManager.RemoteEventQueue.Enqueue(new NetworkManager.RemoteEventPacket()
			{
				Data = SerializationManager.SerializeObject(table, GameManager),
				Recievers = [],
				RemoteEventId = UniqueID
			});
		}
		[Lua([Security.Capability.None])]
		public void FireClient(Player plr, DynValue table)
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new Exception("Cannot call FireClient on client!");

			GameManager.NetworkManager.RemoteEventQueue.Enqueue(new NetworkManager.RemoteEventPacket()
			{
				Data = SerializationManager.SerializeObject(table, GameManager),
				Recievers = [plr.Client ?? throw new Exception("This Player is not supported")], // say what
				RemoteEventId = UniqueID
			});
		}
		[Lua([Security.Capability.None])]
		public void FireAllClients(DynValue table)
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new Exception("Cannot call FireAllClients on client!");

			GameManager.NetworkManager.RemoteEventQueue.Enqueue(new NetworkManager.RemoteEventPacket()
			{
				Data = SerializationManager.SerializeObject(table, GameManager),
				Recievers = GameManager.NetworkManager.Clients.ToArray(),
				RemoteEventId = UniqueID
			});
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RemoteEvent) == classname) return true;
			return base.IsA(classname);
		}
	}
}
