namespace NetBlox.Network;

public enum NetworkPacketType
{
    NPServerboundHandshake, NPClientboundHandshake, NPKick, NPRequestInitialReplication, NPRespondInitialReplication, NPPhysicsUpdate,
    NPUnreliableConnectionInfo, NPRespondDeltaReplication, NPPing, NPClientCameraUpdate
}