using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetBlox
{
    public static class NetworkUtils
    {
        public const byte PacketStorageNumber = 1;
        public const byte PacketStorageString = 2;
        public const byte PacketStorageBytes = 4;

        public static NetworkPacket Packet(byte type, long num = 0, string? str = null, byte[]? bytes = null)
        {
            NetworkPacket packet = new();

            packet.PacketType = type;
            packet.PacketStorage = (byte)((num == 0 ? 1 : 0) | (str == null ? 2 : 0) | (bytes == null ? 4 : 0));

            packet.AuxNumber = num;
            packet.AuxString = str == null ? string.Empty : str;
            packet.AuxBytes = bytes == null ? Array.Empty<byte>() : bytes;

            return packet;
        }

        public static bool WaitForData(NetworkStream ns, int timeout = 1000)
        {
            DateTime time = DateTime.Now.AddMilliseconds(timeout);

            while (!ns.DataAvailable && DateTime.Now < time) ;
            return ns.DataAvailable;
        }

        public static NetworkPacket ReadPacket(NetworkStream ns)
        {
            List<byte> bytes = new();
            int lastbyte = ns.ReadByte();

            while (lastbyte != -1)
            {
                bytes.Add((byte)lastbyte);
                lastbyte = ns.ReadByte();
            }

            return ReadPacket(bytes.ToArray());
        }

        public static NetworkPacket ReadPacket(byte[] data)
        {
            NetworkPacket p = new();
            using MemoryStream m = new(data);
            using BinaryReader r = new(m);

            p.PacketType = r.ReadByte();
            p.PacketStorage = r.ReadByte();

            if ((p.PacketStorage & PacketStorageNumber) != 0)
                p.AuxNumber = r.ReadInt64();

            if ((p.PacketStorage & PacketStorageString) != 0)
                p.AuxString = Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));

            if ((p.PacketStorage & PacketStorageBytes) != 0)
                p.AuxBytes = r.ReadBytes(r.ReadInt32());

            return p;
        }

        public static void WritePacket(NetworkStream ns, NetworkPacket packet)
        {
            ns.Write(WritePacket(packet));
        }

        public static byte[] WritePacket(NetworkPacket p)
        {
            using MemoryStream m = new();
            using BinaryWriter w = new(m);

            w.Write(p.PacketType);
            w.Write(p.PacketStorage);

            if ((p.PacketStorage & PacketStorageNumber) != 0)
                w.Write(p.AuxNumber);

            if ((p.PacketStorage & PacketStorageString) != 0)
            {
                byte[] str = Encoding.UTF8.GetBytes(p.AuxString);
                w.Write(str.Length);
                w.Write(str);
            }

            if ((p.PacketStorage & PacketStorageBytes) != 0)
            {
                w.Write(p.AuxBytes.Length);
                w.Write(p.AuxBytes);
            }

            return m.ToArray();
        }
    }
    public class NetworkPacket
    {
        public byte PacketType;
        public byte PacketStorage;
        public long AuxNumber;
        public string AuxString = string.Empty;
        public byte[] AuxBytes = Array.Empty<byte>();
    }
}
