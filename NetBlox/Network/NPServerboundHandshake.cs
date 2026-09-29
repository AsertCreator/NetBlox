using System.Diagnostics;
using NetBlox.Instances.Services;

namespace NetBlox.Network;

public sealed class NPServerboundHandshake : NetworkPacketHandler
{
    public record struct Entity
    {
        public int ProtocolVersion;
        public short VersionMajor;
        public short VersionMinor;
        public short VersionPatch;
        public ClientFlags EvaluatedClientFlags;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.ProtocolVersion);
        bw.Write(entity.VersionMajor);
        bw.Write(entity.VersionMinor);
        bw.Write(entity.VersionPatch);
        bw.Write((short)entity.EvaluatedClientFlags);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPServerboundHandshake,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();

        Entity entity = default;
        entity.ProtocolVersion = br.ReadInt32();
        entity.VersionMajor = br.ReadInt16();
        entity.VersionMinor = br.ReadInt16();
        entity.VersionPatch = br.ReadInt16();
        entity.EvaluatedClientFlags = (ClientFlags)br.ReadInt16();

        if (entity.ProtocolVersion != PROTOCOL_VERSION)
        {
            networkServer.ReportVersionMismatch(entity, player);
            return;
        }

        var task = gm.GameScheduler.Schedule("Setting up Player instance", GameScheduler.SchedulerPhase.Network, _ =>
        {
            bool isGuest = (entity.EvaluatedClientFlags & ClientFlags.Authenticated) == 0;

            NPClientboundHandshake.Entity response = default;
            response.ServerVersionMajor = Version.VersionMajor;
            response.ServerVersionMinor = Version.VersionMinor;
            response.ServerVersionPatch = Version.VersionPatch;
            response.JobId = gm.RootModel.JobId;
            response.PlaceId = gm.RootModel.PlaceId;
            response.PlaceName = gm.RootModel.Name;
            response.PlaceVersion = gm.RootModel.PlaceVersion;
            response.UniverseId = gm.RootModel.GameId;
            response.UniverseName = gm.RootModel.UniverseName;
            response.AuthorId = gm.RootModel.AuthorId;

            string resultingUsername = ""; // TODO: authentication
            Players players = gm.RootModel.GetService<Players>();

            if (isGuest)
            {
                do
                    resultingUsername = "Guest " + Random.Shared.Next(10, 10000);
                while (players.FindFirstChild(resultingUsername) != null);
            }

            player.Name = resultingUsername;
            player.Parent = players;

            response.YourUsername = resultingUsername;

            if (player.Connection == null)
            {
                Trace.TraceError("Something went horribly wrong with Player bookkeeping, imma die");
                return SchedulerTaskResult.CompletedFailed;
            }

            player.Connection.SendPacketReliable(NPClientboundHandshake.Create(response));

            Trace.TraceInformation("Player connected: " + response.YourUsername);

            return SchedulerTaskResult.CompletedSuccess;
        });
        task.Identity = Runtime.SecurityIdentity.SI_EngineNetworker;
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        networkClient.ReportInvalidPacket(networkPacket);
    }
}