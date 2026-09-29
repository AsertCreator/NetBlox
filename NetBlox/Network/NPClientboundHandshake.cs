using System.Diagnostics;

namespace NetBlox.Network;

public sealed class NPClientboundHandshake : NetworkPacketHandler
{
    public record struct Entity
    {
        public short ServerVersionMajor;
        public short ServerVersionMinor;
        public short ServerVersionPatch;
        public string PlaceName;
        public string UniverseName;
        public long PlaceId;
        public long UniverseId;
        public long AuthorId;
        public string YourUsername;
        public string JobId;
        public long PlaceVersion;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.ServerVersionMajor);
        bw.Write(entity.ServerVersionMinor);
        bw.Write(entity.ServerVersionPatch);
        bw.Write(entity.PlaceName);
        bw.Write(entity.UniverseName);
        bw.Write(entity.PlaceId);
        bw.Write(entity.UniverseId);
        bw.Write(entity.AuthorId);
        bw.Write(entity.YourUsername);
        bw.Write(entity.JobId);
        bw.Write(entity.PlaceVersion);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPClientboundHandshake,
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

        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();

        Entity entity = default;
        entity.ServerVersionMajor = br.ReadInt16();
        entity.ServerVersionMinor = br.ReadInt16();
        entity.ServerVersionPatch = br.ReadInt16();
        entity.PlaceName = br.ReadString();
        entity.UniverseName = br.ReadString();
        entity.PlaceId = br.ReadInt64();
        entity.UniverseId = br.ReadInt64();
        entity.AuthorId = br.ReadInt64();
        entity.YourUsername = br.ReadString();
        entity.JobId = br.ReadString();
        entity.PlaceVersion = br.ReadInt64();

        gm.RootModel.PlaceId = entity.PlaceId;
        gm.RootModel.GameId = entity.UniverseId;
        gm.RootModel.Name = entity.PlaceName;
        gm.RootModel.JobId = entity.JobId;
        gm.RootModel.UniverseName = entity.UniverseName;
        gm.RootModel.AuthorId = entity.AuthorId;
        gm.RootModel.PlaceVersion = entity.PlaceVersion;

        networkClient.CurrentServerVersionString = $"{entity.ServerVersionMajor}.{entity.ServerVersionMinor}.{entity.ServerVersionPatch}";
        networkClient.CurrentUsername = entity.YourUsername;

        Trace.TraceInformation("Assigned username is " + networkClient.CurrentUsername);

        NPRequestInitialReplication.Entity entity1 = default;
        entity1.Flags = 0;

        connection.SendPacketReliable(NPRequestInitialReplication.Create(entity1));
    }
}