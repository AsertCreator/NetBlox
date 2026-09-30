using System.Numerics;

namespace NetBlox.Network;

public sealed class NPClientCameraUpdate : NetworkPacketHandler
{
    public static NetworkPacket Create(Vector3 vector3, Vector3 lookAt)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(vector3.X);
        bw.Write(vector3.Y);
        bw.Write(vector3.Z);
        bw.Write(lookAt.X);
        bw.Write(lookAt.Y);
        bw.Write(lookAt.Z);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPClientCameraUpdate,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        Vector3 position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        Vector3 lookat = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        player.CurrentCameraPosition = position;
        player.CurrentCameraLookAt = lookat;
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        networkClient.ReportInvalidPacket(networkPacket);
    }
}