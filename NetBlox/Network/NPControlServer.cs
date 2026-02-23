using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System.Diagnostics;

namespace NetBlox.Network
{
	public enum NPControlServerType
	{
		None, KillServer, RunCode
	}
	public class NPControlServer : NetworkPacketHandler
	{
		public override int ProbeTargetPacketId => TargetPacketId;

		public const int TargetPacketId = (int)NetworkPacketTypeEnum.NPControlServer;

		public static NetworkPacket Create(NPControlServerType type, string? parameter0, string? parameter1)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write((int)type);
			writer.Write(parameter0 ?? "");
			writer.Write(parameter1 ?? "");

			return new NetworkPacket(TargetPacketId, stream.ToArray(), null);
		}

		public override void HandleClientbound(GameManager gm, NetworkPacket packet, BinaryReader reader) { }
		public override void HandleServerbound(GameManager gm, NetworkPacket packet, BinaryReader reader)
		{
			if (!Debugger.IsAttached) // what if ddos
				return;

			var type = (NPControlServerType)reader.ReadInt32();
			var parameter0 = reader.ReadString();
			var parameter1 = reader.ReadString();

			switch (type)
			{
				case NPControlServerType.KillServer:
					gm.Shutdown();
					break;
				case NPControlServerType.RunCode:
					TaskScheduler.ScheduleScript(gm, parameter0, 8, null);
					break;
			}
		}
	}
}
