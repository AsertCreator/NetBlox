using NetBlox.Instances;
using NetBlox.Runtime;

namespace NetBlox.Network;

public record struct RemoteFunctionAwaiterItem
{
    public required RemoteFunction Function;
    public required ScriptSchedulerTask ScriptSchedulerTask;
    public required int Nonce;
}