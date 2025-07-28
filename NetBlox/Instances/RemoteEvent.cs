using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
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

		public RemoteEvent(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public void FireServer(DynValue table)
		{
			if (!GameManager.NetworkManager.IsClient)
				throw new ScriptRuntimeException("Cannot call FireServer on server!");

			NetworkPacket packet = NPRemoteEvent.Create(this, table);
			GameManager.NetworkManager.SendServerboundPacket(packet);
		}
		[Lua([Security.Capability.None])]
		public void FireClient(Player plr, DynValue table)
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new ScriptRuntimeException("Cannot call FireClient on client!");

			NetworkPacket packet = NPRemoteEvent.Create(this, table);
			plr.Client.SendPacket(packet);
		}
		[Lua([Security.Capability.None])]
		public void FireAllClients(DynValue table)
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new ScriptRuntimeException("Cannot call FireAllClients on client!");

			NetworkPacket packet = NPRemoteEvent.Create(this, table);

			for (int i = 0; i < GameManager.NetworkManager.Clients.Count; i++)
			{
				var rc = GameManager.NetworkManager.Clients[i];
				rc.SendPacket(packet);
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RemoteEvent) == classname) return true;
			return base.IsA(classname);
		}
	}
}
