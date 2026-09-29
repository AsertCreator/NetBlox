using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using NetBlox.Instances.Parts;

namespace NetBlox.Network;

public sealed class NPPhysicsUpdate : NetworkPacketHandler
{
    public static NetworkPacket Create(List<NetworkServer.BasePartPhysicsUpdateItem> allitems)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(allitems.Count);

        for (int i = 0; i < allitems.Count; i++)
        {
            bw.Write(allitems[i].Target.InstanceID);
            bw.Write(allitems[i].NewPosition.X);
            bw.Write(allitems[i].NewPosition.Y);
            bw.Write(allitems[i].NewPosition.Z);
            bw.Write(allitems[i].NewOrientation.X);
            bw.Write(allitems[i].NewOrientation.Y);
            bw.Write(allitems[i].NewOrientation.Z);
            bw.Write(allitems[i].NewLinearVelocity.X);
            bw.Write(allitems[i].NewLinearVelocity.Y);
            bw.Write(allitems[i].NewLinearVelocity.Z);
            bw.Write(allitems[i].NewAngularVelocity.X);
            bw.Write(allitems[i].NewAngularVelocity.Y);
            bw.Write(allitems[i].NewAngularVelocity.Z);
        }

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPPhysicsUpdate,
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

        int count = br.ReadInt32();

        if (gm.PhysicsSolver == null)
            return;

        lock (gm.PhysicsSolver)
        {
            for (int i = 0; i < count; i++)
            {
                InstanceID instanceId = br.ReadUInt64();
                BasePart? basePart = gm.GameRegistry.GetLocalInstanceById(instanceId) as BasePart;

                if (basePart == null)
                {
                    ms.Position += 4 * 3 * 4;
                    continue;
                }

                basePart.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                basePart.Rotation = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                basePart.Velocity = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                basePart.AngularVelocity = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                basePart.UpdateState();
            }
        }
    }
}