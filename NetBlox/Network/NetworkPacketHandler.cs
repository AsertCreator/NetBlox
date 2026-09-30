namespace NetBlox.Network;

public abstract class NetworkPacketHandler
{
    private static Dictionary<int, NetworkPacketHandler> handlers = new()
    {
        [(int)NetworkPacketType.NPServerboundHandshake] = new NPServerboundHandshake(),
        [(int)NetworkPacketType.NPClientboundHandshake] = new NPClientboundHandshake(),
        [(int)NetworkPacketType.NPKick] = new NPKick(),
        [(int)NetworkPacketType.NPRequestInitialReplication] = new NPRequestInitialReplication(),
        [(int)NetworkPacketType.NPRespondInitialReplication] = new NPRespondInitialReplication(),
        [(int)NetworkPacketType.NPPhysicsUpdate] = new NPPhysicsUpdate(),
        [(int)NetworkPacketType.NPUnreliableConnectionInfo] = new NPUnreliableConnectionInfo(),
        [(int)NetworkPacketType.NPRespondDeltaReplication] = new NPRespondDeltaReplication(),
        [(int)NetworkPacketType.NPPing] = new NPPing(),
    };

    public const int PROTOCOL_VERSION = 1;

    public static NetworkPacketHandler? ForPacketType(int targetType)
    {
        handlers.TryGetValue(targetType, out NetworkPacketHandler? handler);
        return handler;
    }

    public abstract void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player);
    public abstract void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection);
}