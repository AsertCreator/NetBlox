using System.Diagnostics;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Network;

public sealed class NPRemoteFunctionResponse : NetworkPacketHandler
{
    public static NetworkPacket Create(RemoteFunction remoteFunction, int nonce, DynValue result, string? error)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(error != null);
        bw.Write(remoteFunction.InstanceID);
        bw.Write(nonce);
        if (error != null)
            bw.Write(error);
        bw.Write(LuaMarshal.SerializeLuaToBytes(result));

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRemoteFunctionResponse,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        bool isErrored = br.ReadBoolean();

        RemoteFunction? function = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as RemoteFunction;
        if (function == null)
        {
            Trace.TraceWarning("Recevied a response to a RemoteFunction that didn't arrive yet!");
            return;
        }

        int nonce = br.ReadInt32();
        string? error = null;
        if (isErrored)
            error = br.ReadString();

        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));

        // we need to get the awaiter

        RemoteFunctionAwaiterItem awaiterItem = networkServer.RemoteFunctionAwaiterItems[nonce];
        DynValue dynValue = LuaMarshal.DeserializeLuaFromBytes(payload, scriptContext, awaiterItem.ScriptSchedulerTask.Identity!);
        
        networkServer.RemoteFunctionAwaiterItems.Remove(nonce);

        awaiterItem.ScriptSchedulerTask.AsyncFunctionArguments = dynValue;
        awaiterItem.ScriptSchedulerTask.IsWaiting = false;

        if (error != null)
        {
            awaiterItem.ScriptSchedulerTask.ErrorOnReexecution = true;
            awaiterItem.ScriptSchedulerTask.ErrorOnReexecutionDetails = error;
        }
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        bool isErrored = br.ReadBoolean();

        RemoteFunction? function = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as RemoteFunction;
        if (function == null)
        {
            Trace.TraceWarning("Recevied a response to a RemoteFunction that didn't arrive yet!");
            return;
        }

        int nonce = br.ReadInt32();
        string? error = null;
        if (isErrored)
            error = br.ReadString();

        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));

        // we need to get the awaiter

        RemoteFunctionAwaiterItem awaiterItem = networkClient.RemoteFunctionAwaiterItems[nonce];
        DynValue dynValue = LuaMarshal.DeserializeLuaFromBytes(payload, scriptContext, awaiterItem.ScriptSchedulerTask.Identity!);
        
        networkClient.RemoteFunctionAwaiterItems.Remove(nonce);

        awaiterItem.ScriptSchedulerTask.AsyncFunctionArguments = dynValue;
        awaiterItem.ScriptSchedulerTask.IsWaiting = false;

        if (error != null)
        {
            awaiterItem.ScriptSchedulerTask.ErrorOnReexecution = true;
            awaiterItem.ScriptSchedulerTask.ErrorOnReexecutionDetails = error;
        }
    }
}