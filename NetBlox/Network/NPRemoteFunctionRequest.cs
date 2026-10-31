using System.Diagnostics;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Network;

public sealed class NPRemoteFunctionRequest : NetworkPacketHandler
{
    public static NetworkPacket Create(RemoteFunction remoteFunction, int nonce, DynValue arguments)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(remoteFunction.InstanceID);
        bw.Write(nonce);
        bw.Write(LuaMarshal.SerializeLuaToBytes(arguments));

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRemoteFunctionRequest,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        RemoteFunction? function = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as RemoteFunction;
        if (function == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteFunction that didn't arrive yet!");
            return;
        }
        if (function.OnServerInvokeSecurityIdentity == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteFunction that has no OnServerInvoke set!");
            return;
        }

        int nonce = br.ReadInt32();
        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));
        DynValue dynValue = LuaMarshal.DeserializeLuaFromBytes(payload, scriptContext, function.OnServerInvokeSecurityIdentity);
        List<DynValue> arguments = [];

        if (dynValue.Type == DataType.Table)
            arguments.AddRange(dynValue.Table.Values.ToArray());
        else if (dynValue.Type == DataType.Tuple)
            arguments.AddRange(dynValue.Tuple);
        
        arguments.Insert(0, DynValue.NewUserData(InstanceBridge.PushUserData(gm, player)));
        
        Script script = scriptContext.GetLuaStateFor(function.OnServerInvokeSecurityIdentity);
        ScriptSchedulerTask task = (ScriptSchedulerTask)gm.GameScheduler.Schedule(new ScriptSchedulerTask(null, function.realOnServerInvoke)
        {
            DebugName = $"RemoteFunction Handler - {function.GetFullName()} - {nonce}",
            GameScheduler = gm.GameScheduler,
            Delegate = _ => SchedulerTaskResult.NotCompleted,
            AsyncFunctionArguments = DynValue.NewTuple(arguments.ToArray())
        });

        task.Identity = function.OnServerInvokeSecurityIdentity;
        task.CallbackAfterSuccessfulExecution = () =>
        {
            DynValue result = task.AsyncFunctionArguments;
            NetworkPacket nPRemoteFunctionResponse = NPRemoteFunctionResponse.Create(function, nonce, result, null);

            player.Connection!.SendPacketReliable(nPRemoteFunctionResponse);
        };
        task.CallbackAfterFailedExecution = () =>
        {
            NetworkPacket nPRemoteFunctionResponse = NPRemoteFunctionResponse.Create(function, nonce, DynValue.Nil, "RemoteFunction errored out");

            player.Connection!.SendPacketReliable(nPRemoteFunctionResponse);
        };
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        ScriptContext scriptContext = gm.RootModel.GetService<ScriptContext>();

        RemoteFunction? function = gm.GameRegistry.GetLocalInstanceById(br.ReadUInt64()) as RemoteFunction;
        if (function == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteFunction that didn't arrive yet!");
            return;
        }
        if (function.OnClientInvokeSecurityIdentity == null)
        {
            Trace.TraceWarning("Recevied a call to a RemoteFunction that has no OnClientInvoke set!");
            return;
        }

        int nonce = br.ReadInt32();
        byte[] payload = br.ReadBytes((int)(ms.Length - ms.Position));
        DynValue dynValue = LuaMarshal.DeserializeLuaFromBytes(payload, scriptContext, function.OnClientInvokeSecurityIdentity);
        List<DynValue> arguments = [];

        if (dynValue.Type == DataType.Table)
            arguments.AddRange(dynValue.Table.Values.ToArray());
        else if (dynValue.Type == DataType.Tuple)
            arguments.AddRange(dynValue.Tuple);
        
        Script script = scriptContext.GetLuaStateFor(function.OnClientInvokeSecurityIdentity);
        ScriptSchedulerTask task = (ScriptSchedulerTask)gm.GameScheduler.Schedule(new ScriptSchedulerTask(null, function.realOnClientInvoke)
        {
            DebugName = $"RemoteFunction Handler - {function.GetFullName()} - {nonce}",
            GameScheduler = gm.GameScheduler,
            Delegate = _ => SchedulerTaskResult.NotCompleted,
            AsyncFunctionArguments = DynValue.NewTuple(arguments.ToArray())
        });

        task.Identity = function.OnClientInvokeSecurityIdentity;
        task.CallbackAfterSuccessfulExecution = () =>
        {
            DynValue result = task.AsyncFunctionArguments;
            NetworkPacket nPRemoteFunctionResponse = NPRemoteFunctionResponse.Create(function, nonce, result, null);

            connection.SendPacketReliable(nPRemoteFunctionResponse);
        };
        task.CallbackAfterFailedExecution = () =>
        {
            NetworkPacket nPRemoteFunctionResponse = NPRemoteFunctionResponse.Create(function, nonce, DynValue.Nil, "RemoteFunction errored out");

            connection.SendPacketReliable(nPRemoteFunctionResponse);
        };
    }
}