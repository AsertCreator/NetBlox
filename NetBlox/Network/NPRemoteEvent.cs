using System.Diagnostics;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Network;

public sealed class NPRemoteEvent : NetworkPacketHandler
{
    public static NetworkPacket Create(BaseRemoteEvent remoteEvent, DynValue arguments)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(remoteEvent.InstanceID);
        bw.Write(LuaMarshal.SerializeLuaToBytes(arguments));

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRemoteEvent,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        BaseRemoteEvent? remoteEvent = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as BaseRemoteEvent;
        if (remoteEvent == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteEvent that didn't arrive yet!");
            return;
        }

        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));

        if (!remoteEvent.OnServerEvent.HasConnections && !remoteEvent.OnServerEvent.HasWaiters)
            return; // or maybe not

        remoteEvent.OnServerEvent.FireWithNetworkLuaPayload(player, payload);
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkClient networkServer = gm.RootModel.GetService<NetworkClient>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        BaseRemoteEvent? remoteEvent = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as BaseRemoteEvent;
        if (remoteEvent == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteEvent that didn't arrive yet!");
            return;
        }

        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));

        if (!remoteEvent.OnClientEvent.HasConnections && !remoteEvent.OnClientEvent.HasWaiters)
            return; // or maybe not

        remoteEvent.OnClientEvent.FireWithNetworkLuaPayload(payload);
    }
}