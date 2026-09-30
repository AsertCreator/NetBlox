using System.Numerics;
using NetBlox.Instances;
using NetBlox.Structs;

namespace NetBlox.Network;

public static class NetworkMarshal
{
    public static void MarshalClrToNetwork(GameManager gameManager, BinaryWriter writer, Type clrType, object? value)
    {
        if (value != null)
            writer.Write(true);
        else
        {
            writer.Write(false);
            return;
        }

        if (clrType == cachedInt8Type)
            writer.Write((sbyte)value);
        else if (clrType == cachedInt16Type)
            writer.Write((short)value);
        else if (clrType == cachedInt32Type)
            writer.Write((int)value);
        else if (clrType == cachedInt64Type)
            writer.Write((long)value);
        else if (clrType == cachedUInt8Type)
            writer.Write((byte)value);
        else if (clrType == cachedUInt16Type)
            writer.Write((ushort)value);
        else if (clrType == cachedUInt32Type)
            writer.Write((uint)value);
        else if (clrType == cachedUInt64Type)
            writer.Write((ulong)value);
        else if (clrType == cachedSingleType)
            writer.Write((float)value);
        else if (clrType == cachedDoubleType)
            writer.Write((double)value);
        else if (clrType == cachedStringType)
            writer.Write((string)value);
        else if (clrType == cachedBooleanType)
            writer.Write((bool)value);
        else if (clrType.IsAssignableTo(cachedInstanceType))
            writer.Write(((Instance)value).InstanceID);
        else if (clrType == cachedVector2Type)
        {
            Vector2 vector2 = (Vector2)value;
            writer.Write(vector2.X);
            writer.Write(vector2.Y);
        }
        else if (clrType == cachedVector3Type)
        {
            Vector3 vector3 = (Vector3)value;
            writer.Write(vector3.X);
            writer.Write(vector3.Y);
            writer.Write(vector3.Z);
        }
        else if (clrType == cachedUDimType)
        {
            UDim udim = (UDim)value;
            writer.Write(udim.Scale);
            writer.Write(udim.Offset);
        }
        else if (clrType == cachedUDim2Type)
        {
            UDim2 udim2 = (UDim2)value;
            writer.Write(udim2.X.Scale);
            writer.Write(udim2.X.Offset);
            writer.Write(udim2.Y.Scale);
            writer.Write(udim2.Y.Offset);
        }
        else if (clrType == cachedColor3Type)
            writer.Write(Color3.ToBGRA((Color3)value));
        else if (clrType == cachedBrickColorType)
            writer.Write(((BrickColor)value).Index);
        else if (clrType.IsEnum)
        {
            writer.Write((int)value);
        }
        else
            throw new Exception("Unsupported network type");
    }
    public static object? MarshalNetworkToClr(GameManager gameManager, BinaryReader reader, Type clrType)
    {
        if (!reader.ReadBoolean())
            return null;

        if (clrType == cachedInt8Type)
        {
            return reader.ReadSByte();
        }
        else if (clrType == cachedInt16Type)
        {
            return reader.ReadInt16();
        }
        else if (clrType == cachedInt32Type)
        {
            return reader.ReadInt32();
        }
        else if (clrType == cachedInt64Type)
        {
            return reader.ReadInt64();
        }
        else if (clrType == cachedUInt8Type)
        {
            return reader.ReadByte();
        }
        else if (clrType == cachedUInt16Type)
        {
            return reader.ReadUInt16();
        }
        else if (clrType == cachedUInt32Type)
        {
            return reader.ReadUInt32();
        }
        else if (clrType == cachedUInt64Type)
        {
            return reader.ReadUInt64();
        }
        else if (clrType == cachedSingleType)
        {
            return reader.ReadSingle();
        }
        else if (clrType == cachedDoubleType)
        {
            return reader.ReadDouble();
        }
        else if (clrType == cachedStringType)
        {
            return reader.ReadString();
        }
        else if (clrType == cachedBooleanType)
        {
            return reader.ReadBoolean();
        }
        else if (clrType.IsAssignableTo(cachedInstanceType))
        {
            return gameManager.GameRegistry.GetLocalInstanceById(reader.ReadUInt64());
        }
        else if (clrType == cachedVector2Type)
        {
            return new Vector2()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),  
            };
        }
        else if (clrType == cachedVector3Type)
        {
            return new Vector3()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle(),
            };
        }
        else if (clrType == cachedUDimType)
        {
            return new UDim()
            {
                Scale = reader.ReadSingle(),
                Offset = reader.ReadSingle(),  
            };
        }
        else if (clrType == cachedUDim2Type)
        {
            return new UDim2()
            {
                X = new UDim()
                {
                    Scale = reader.ReadSingle(),
                    Offset = reader.ReadSingle(),
                },
                Y = new UDim()
                {
                    Scale = reader.ReadSingle(),
                    Offset = reader.ReadSingle(),
                },
            };
        }
        else if (clrType == cachedColor3Type)
        {
            return Color3.FromBGRA(reader.ReadUInt32());
        }
        else if (clrType == cachedBrickColorType)
        {
            return BrickColor.GetBrickColorByIndex(reader.ReadInt32());
        }
        else if (clrType.IsEnum)
        {
            return Enum.ToObject(clrType, reader.ReadInt32());
        }
        else
            throw new Exception("Unsupported network type");
    }

    private static readonly Type cachedInt8Type = typeof(SByte);
    private static readonly Type cachedInt16Type = typeof(Int16);
    private static readonly Type cachedInt32Type = typeof(Int32);
    private static readonly Type cachedInt64Type = typeof(Int64);
    private static readonly Type cachedUInt8Type = typeof(Byte);
    private static readonly Type cachedUInt16Type = typeof(UInt16);
    private static readonly Type cachedUInt32Type = typeof(UInt32);
    private static readonly Type cachedUInt64Type = typeof(UInt64);
    private static readonly Type cachedSingleType = typeof(Single);
    private static readonly Type cachedDoubleType = typeof(Double);
    private static readonly Type cachedStringType = typeof(String);
    private static readonly Type cachedBooleanType = typeof(Boolean);
    private static readonly Type cachedInstanceType = typeof(Instance);
    private static readonly Type cachedVector2Type = typeof(Vector2);
    private static readonly Type cachedVector3Type = typeof(Vector3);
    private static readonly Type cachedUDimType = typeof(UDim);
    private static readonly Type cachedUDim2Type = typeof(UDim2);
    private static readonly Type cachedColor3Type = typeof(Color3);
    private static readonly Type cachedBrickColorType = typeof(BrickColor);
}