namespace NetBlox.Structs;

public enum ContentIdProtocol
{
    NetBloxInternalAsset, NetBloxLocalFile, NetBloxNetworkAsset, RbxAsset
}
public struct ContentId
{
    public required ContentIdProtocol Protocol { get; init; }
    public long? AssetId { get; init; }
    public string? AssetPath { get; init; }

    public override string ToString()
    {
        string protocolString;

        switch (Protocol)
        {
            case ContentIdProtocol.NetBloxInternalAsset:
                protocolString = "net-internal";
                break;
            case ContentIdProtocol.NetBloxLocalFile:
                protocolString = "net-local";
                break;
            case ContentIdProtocol.NetBloxNetworkAsset:
                protocolString = "net-network";
                break;
            default:
            case ContentIdProtocol.RbxAsset:
                protocolString = "rbxasset";
                break;
        }

        if (AssetId.HasValue)
            return protocolString + "://" + AssetId.Value;

        return protocolString + "://" + AssetPath ?? "";
    }
}