namespace NetBlox.Network;

public sealed class NPRequestInitialReplication : NetworkPacketHandler
{
    public record struct Entity
    {
        public int Flags;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.Flags);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRequestInitialReplication,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        int replicationFlags = br.ReadInt32();

        gm.GameScheduler.Schedule("Initial Replication Preparation", GameScheduler.SchedulerPhase.Network, _ =>
        {
            NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
            ReplicationAgent agent = networkServer.ReplicationAgent;

            if (player.Connection == null)
                return SchedulerTaskResult.CompletedFailed;
            
            player.hadInitialReplication = true;

            byte[] payload = agent.ConstructInitialReplicationFrame();

            NPRespondInitialReplication.Entity entity = default;
            entity.Flags = 0;
            entity.YourLocalPlayer = player.InstanceID;
            entity.Payload = payload;

            player.Connection.SendPacketReliable(NPRespondInitialReplication.Create(entity));
            player.hadInitialReplication = true;

            return SchedulerTaskResult.CompletedSuccess;
        });
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        networkClient.ReportInvalidPacket(networkPacket);
    }
}