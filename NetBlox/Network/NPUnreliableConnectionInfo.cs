using System.Net;
using System.Net.Sockets;

namespace NetBlox.Network;

public sealed class NPUnreliableConnectionInfo : NetworkPacketHandler
{
    public record struct Entity
    {
        public ushort UdpPort;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.UdpPort);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPUnreliableConnectionInfo,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        Entity entity = default;
        entity.UdpPort = br.ReadUInt16();

        player.Connection!.UdpClientEndpoint = new IPEndPoint(player.Connection.EndPoint!.Address.MapToIPv4(), entity.UdpPort);
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        Entity entity = default;
        entity.UdpPort = br.ReadUInt16();

        var task = gm.GameScheduler.Schedule("Initiate Unreliable Connection", GameScheduler.SchedulerPhase.Network, _ =>
        {
            NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
            IPEndPoint iPEndPoint = new IPEndPoint(connection.EndPoint!.Address.MapToIPv4(), entity.UdpPort);

            connection.ConnectUnreliableToEndpoint(iPEndPoint);

            NPUnreliableConnectionInfo.Entity entity1 = default;
            IPEndPoint? localudpendpoint = connection.UdpClient!.Client.LocalEndPoint as IPEndPoint;

            entity1.UdpPort = (ushort)localudpendpoint!.Port;

            connection.UdpClientEndpoint = iPEndPoint;
            connection.SendPacketReliable(NPUnreliableConnectionInfo.Create(entity1));

            NPServerboundHandshake.Entity entity2 = default;

            entity2.VersionMajor = Version.VersionMajor;
            entity2.VersionMinor = Version.VersionMinor;
            entity2.VersionPatch = Version.VersionPatch;
            entity2.EvaluatedClientFlags = ClientFlagsExtension.Evaluate(gm);
            entity2.ProtocolVersion = PROTOCOL_VERSION;

            connection.SendPacketReliable(NPServerboundHandshake.Create(entity2));

            return SchedulerTaskResult.CompletedSuccess; 
        });
        task.Identity = Runtime.SecurityIdentity.SI_EngineNetworker;
    }
}