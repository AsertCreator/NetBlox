using NetBlox.Instances.Services;

namespace NetBlox.Network;

public sealed class NPRespondInitialReplication : NetworkPacketHandler
{
    public record struct Entity
    {
        public int Flags;
        public ulong YourLocalPlayer;
        public byte[] Payload;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.Flags);
        bw.Write(entity.YourLocalPlayer);
        bw.Write(entity.Payload.Length);
        bw.Write(entity.Payload);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRespondInitialReplication,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
        networkServer.ReportInvalidPacket(networkPacket, player);
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        int replicationFlags = br.ReadInt32();
        ulong ourLocalPlayer = br.ReadUInt64();
        int payloadLength = br.ReadInt32();
        byte[] payload = br.ReadBytes(payloadLength);

        var task = gm.GameScheduler.Schedule("Apply Initial Replication", GameScheduler.SchedulerPhase.Network, _ =>
        {
            NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
            ReplicationAgent replicationAgent = networkClient.ReplicationAgent;

            replicationAgent.ApplyInitialReplicationFrame(payload);

            Player? localPlayer = gm.GameRegistry.GetLocalInstanceById(ourLocalPlayer) as Player;
            if (localPlayer == null)
            {
                gm.RootModel.GetService<NetworkClient>().InitiateUnilateralDisconnect("No local Player instance arrived");
                return SchedulerTaskResult.CompletedFailed;
            }
            
            Players players = gm.RootModel.GetService<Players>();
            players.SetLocalPlayer(localPlayer);

            return SchedulerTaskResult.CompletedSuccess; 
        });
        task.Identity = Runtime.SecurityIdentity.SI_EngineNetworker;
    }
}