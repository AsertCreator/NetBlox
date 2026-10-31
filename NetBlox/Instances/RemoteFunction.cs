using MoonSharp.Interpreter;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(74)]
public class RemoteFunction : Instance
{
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public DynValue OnServerInvoke 
    { 
        get
        {
            if (realOnServerInvoke.IsNil())
                return DynValue.Nil;
            return DynValue.NewCallback((_, _) =>
            {
                throw new InvalidOperationException("Do not call RemoteFunction.OnServerInvoke directly");
            });
        }
        set
        {
            if (GameManager.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot set OnServerInvoke on a client");
            if (value.IsNil() || value.Type == DataType.Function)
            {
                OnServerInvokeSecurityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity();
                realOnServerInvoke = value;
                return;
            }
            throw new InvalidOperationException("RemoteFunction.OnServerInvoke can only be a function or nil");
        }
    }
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public DynValue OnClientInvoke 
    { 
        get
        {
            if (realOnClientInvoke.IsNil())
                return DynValue.Nil;
            return DynValue.NewCallback((_, _) =>
            {
                throw new InvalidOperationException("Do not call RemoteFunction.OnClientInvoke directly");
            });
        }
        set
        {
            if (GameManager.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot set OnClientInvoke on a client");
            if (value.IsNil() || value.Type == DataType.Function)
            {
                OnClientInvokeSecurityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity();
                realOnClientInvoke = value;
                return;
            }
            throw new InvalidOperationException("RemoteFunction.OnClientInvoke can only be a function or nil");
        }
    }

    public SecurityIdentity? OnServerInvokeSecurityIdentity { get; private set; }
    public SecurityIdentity? OnClientInvokeSecurityIdentity { get; private set; }

    public DynValue realOnServerInvoke = DynValue.Nil;
    public DynValue realOnClientInvoke = DynValue.Nil;

    public override string ClassName => nameof(RemoteFunction);

    public RemoteFunction(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public object InvokeClient(Player player, params DynValue[] arguments)
    {
        if (GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
            throw new InvalidOperationException("Cannot use RemoteFunction due to a policy");
        if (GameManager.NetworkMode != NetworkMode.Server)
            throw new InvalidOperationException("Cannot call InvokeClient on a client");

        if (player.Connection == null)
            return DynValue.Nil; // let's be nice

        int nonce = Random.Shared.Next();
        DynValue packed = DynValue.NewTable(GameManager.GetCurrentLuaState());

        foreach (DynValue argument in arguments)
            packed.Table.Append(argument);

        NetworkServer networkServer = Root.GetService<NetworkServer>();
        networkServer.AddRemoteFunctionAwaiter(this, nonce);

        NetworkPacket networkPacket = NPRemoteFunctionRequest.Create(this, nonce, packed);
        player.Connection.SendPacketReliable(networkPacket);

        GameManager.GameScheduler.CurrentSchedulerTask?.IsWaiting = true;

        return new LuaYield();
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public object InvokeServer(params DynValue[] arguments)
    {
        if (GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
            throw new InvalidOperationException("Cannot use RemoteFunction due to a policy");
        if (GameManager.NetworkMode != NetworkMode.Client)
            throw new InvalidOperationException("Cannot call InvokeServer on a server");

        NetworkClient networkClient = Root.GetService<NetworkClient>();        

        if (networkClient.CurrentServerConnection == null)
            return DynValue.Nil; // let's be nice

        int nonce = Random.Shared.Next();
        DynValue packed = DynValue.NewTable(GameManager.GetCurrentLuaState());

        foreach (DynValue argument in arguments)
            packed.Table.Append(argument);

        networkClient.AddRemoteFunctionAwaiter(this, nonce);

        NetworkPacket networkPacket = NPRemoteFunctionRequest.Create(this, nonce, packed);
        networkClient.CurrentServerConnection.SendPacketReliable(networkPacket);

        GameManager.GameScheduler.CurrentSchedulerTask?.IsWaiting = true;

        return new LuaYield();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(RemoteFunction))
            return base.IsA(className);
        return true;
    }
}