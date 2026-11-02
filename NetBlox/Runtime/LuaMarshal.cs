using System.Numerics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime.Bridges;
using NetBlox.Structs;

namespace NetBlox.Runtime;

public static class LuaMarshal
{
    public static DynValue MarshalClrToLua(string funcName, ScriptContext ctx, object? value)
    {
        if (value == null)
            return DynValue.Nil;

        Type clrType = value.GetType();

        if (clrType.IsArray)
        {
            SecurityIdentity securityIdentity = ctx.GameManager.GameScheduler.GetCurrentSecurityIdentity();
            Script script = ctx.GetLuaStateFor(securityIdentity);
            Table returnvalue = new Table(script);

            Array array = (Array)value;

            for (int i = 0; i < array.Length; i++)
                returnvalue.Append(MarshalClrToLua(funcName, ctx, array.GetValue(i)));
            
            return DynValue.NewTable(returnvalue);
        }

        if (clrType == cachedDynValueType)
            return (DynValue)value;
        else if (clrType == cachedInt8Type)
            return DynValue.NewNumber((sbyte)value);
        else if (clrType == cachedInt16Type)
            return DynValue.NewNumber((short)value);
        else if (clrType == cachedInt32Type)
            return DynValue.NewNumber((int)value);
        else if (clrType == cachedInt64Type)
            return DynValue.NewNumber((long)value);
        else if (clrType == cachedUInt8Type)
            return DynValue.NewNumber((byte)value);
        else if (clrType == cachedUInt16Type)
            return DynValue.NewNumber((ushort)value);
        else if (clrType == cachedUInt32Type)
            return DynValue.NewNumber((uint)value);
        else if (clrType == cachedUInt64Type)
            return DynValue.NewNumber((ulong)value);
        else if (clrType == cachedSingleType)
            return DynValue.NewNumber((float)value);
        else if (clrType == cachedDoubleType)
            return DynValue.NewNumber((double)value);
        else if (clrType == cachedStringType)
            return DynValue.NewString((string)value);
        else if (clrType == cachedBooleanType)
            return DynValue.NewBoolean((bool)value);
        else if (clrType.IsAssignableTo(cachedInstanceType))
        {
            return DynValue.NewUserData(InstanceBridge.PushUserData(ctx.GameManager, (Instance)value));
        }
        else if (clrType == cachedVector2Type)
        {
            return DynValue.NewUserData(Vector2Bridge.PushUserData(ctx.GameManager, (Vector2)value));
        }
        else if (clrType == cachedVector3Type)
        {
            return DynValue.NewUserData(Vector3Bridge.PushUserData(ctx.GameManager, (Vector3)value));
        }
        else if (clrType == cachedUDimType)
        {
            return DynValue.NewUserData(UDimBridge.PushUserData(ctx.GameManager, (UDim)value));
        }
        else if (clrType == cachedUDim2Type)
        {
            return DynValue.NewUserData(UDim2Bridge.PushUserData(ctx.GameManager, (UDim2)value));
        }
        else if (clrType == cachedColor3Type)
        {
            return DynValue.NewUserData(Color3Bridge.PushUserData(ctx.GameManager, (Color3)value));
        }
        else if (clrType == cachedBrickColorType)
        {
            return DynValue.NewUserData(BrickColorBridge.PushUserData(ctx.GameManager, (BrickColor)value));
        }
        else if (clrType == cachedLuaEventType)
        {
            return DynValue.NewUserData(RBXScriptSignalBridge.PushUserData(ctx.GameManager, (LuaEvent)value));
        }
        else if (clrType == cachedLuaYieldType)
        {
            return DynValue.NewYieldReq([]);
        }
        else if (clrType == cachedLuaEventConnectionType)
        {
            return DynValue.NewUserData(RBXScriptConnectionBridge.PushUserData(ctx.GameManager, (LuaEventConnection)value));
        }
        else if (clrType.IsEnum)
        {
            EnumValue enumValue = new EnumValue()
            {
                EnumType = clrType,
                Name = Enum.GetName(clrType, value) ?? "<Unknown Value>",
                Value = (int)value
            };
            return DynValue.NewUserData(EnumValueBridge.PushUserData(ctx.GameManager, enumValue));
        }
        else
            throw new Exception("Unsupported type");
    }
    public static object?[] MarshalLuaToClrArray(string funcName, ScriptContext ctx, DynValue[] array, Type[] parameterTypes)
    {
        List<object?> list = new List<object?>();

        if (array.Length != parameterTypes.Length)
            throw new InvalidOperationException(funcName + " requires " + parameterTypes.Length + " arguments");
        
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            object? obj = MarshalLuaToClr(funcName, ctx, array[i], parameterTypes[i]);
            list.Add(obj);
        }

        return list.ToArray();
    }
    public static object?[] MarshalLuaToClrArraySimple(string funcName, ScriptContext ctx, DynValue[] array, Type arrayElementType)
    {
        List<object?> list = new List<object?>();
        
        for (int i = 0; i < array.Length; i++)
        {
            object? obj = MarshalLuaToClr(funcName, ctx, array[i], arrayElementType);
            list.Add(obj);
        }

        return list.ToArray();
    }
    public static object? MarshalLuaToClr(string funcName, ScriptContext ctx, DynValue value, Type clrType)
    {
        if (value.Type == DataType.Table && clrType.IsArray)
        {
            DynValue[] array = value.Table.Values.ToArray();
            return MarshalLuaToClrArraySimple(funcName, ctx, array, clrType.GetElementType()!);
        }

        if (value.IsNil())
            return null;

        if (clrType == cachedDynValueType)
            return value;
        else if (clrType == cachedInt8Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (sbyte)number;
        }
        else if (clrType == cachedInt16Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (short)number;
        }
        else if (clrType == cachedInt32Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (int)number;
        }
        else if (clrType == cachedInt64Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (long)number;
        }
        else if (clrType == cachedUInt8Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (byte)number;
        }
        else if (clrType == cachedUInt16Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (ushort)number;
        }
        else if (clrType == cachedUInt32Type)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (uint)number;
        }
        else if (clrType == cachedUInt64Type)
        {
            if (value.Type == DataType.UserData && value.UserData.Descriptor is InstanceBridge.InstanceBridgeDescriptor idesc)
                return idesc.Value;
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (ulong)number;
        }
        else if (clrType == cachedSingleType)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return (float)number;
        }
        else if (clrType == cachedDoubleType)
        {
            double number = value.CheckType(funcName, DataType.Number, flags: TypeValidationFlags.None).Number;
            return number;
        }
        else if (clrType == cachedBooleanType)
        {
            bool boolean = value.CheckType(funcName, DataType.Boolean, flags: TypeValidationFlags.None).Boolean;
            return boolean;
        }
        else if (clrType.IsAssignableTo(cachedInstanceType))
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.AllowNil).UserData;
            if (userdata.Descriptor is not InstanceBridge.InstanceBridgeDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes Instance, but not Instance was given");
            return ctx.GameManager.GameRegistry.GetLocalInstanceById(descriptor.Value);
        }
        else if (clrType == cachedVector2Type)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not Vector2Bridge.Vector2Descriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes Vector2, but not Vector2 was given");
            return descriptor.Value;
        }
        else if (clrType == cachedVector3Type)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not Vector3Bridge.Vector3Descriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes Vector3, but not Vector3 was given");
            return descriptor.Value;
        }
        else if (clrType == cachedUDimType)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not UDimBridge.UDimDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes UDim, but not UDim was given");
            return descriptor.Value;
        }
        else if (clrType == cachedUDim2Type)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not UDim2Bridge.UDim2Descriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes UDim2, but not UDim2 was given");
            return descriptor.Value;
        }
        else if (clrType == cachedColor3Type)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not Color3Bridge.Color3Descriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes Color3, but not Color3 was given");
            return descriptor.Value;
        }
        else if (clrType == cachedBrickColorType)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not BrickColorBridge.BrickColorDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes BrickColor, but not BrickColor was given");
            return descriptor.Value;
        }
        else if (clrType == cachedLuaEventType)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not RBXScriptSignalBridge.RBXScriptSignalDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes RBXScriptSignal, but not RBXScriptSignal was given");
            return descriptor.Value;
        }
        else if (clrType == cachedLuaEventConnectionType)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not RBXScriptConnectionBridge.RBXScriptConnectionDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes RBXScriptConnection, but not RBXScriptConnection was given");
            return descriptor.Value;
        }
        else if (clrType.IsEnum)
        {
            UserData userdata = value.CheckType(funcName, DataType.UserData, flags: TypeValidationFlags.None).UserData;
            if (userdata.Descriptor is not EnumValueBridge.EnumValueDescriptor descriptor)
                throw new ArgumentException(funcName + "'s argument takes EnumValue, but not EnumValue was given");
            return Enum.ToObject(clrType, descriptor.Value.Value);
        }
        else if (clrType == cachedStringType)
        {
            return value.ToPrintString();
        }
        else
            throw new Exception("Unsupported type");
    }
    public static DynValue DeserializeLuaFromBytes(byte[] bytes, ScriptContext scriptContext, SecurityIdentity securityIdentity)
    {
        using MemoryStream ms = new MemoryStream(bytes);
        using BinaryReader br = new BinaryReader(ms);

        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        DynValue ProcessItem()
        {
            DataType dataType = (DataType)br.ReadByte();
            switch (dataType)
            {
                case DataType.Number:
                    return DynValue.NewNumber(br.ReadDouble());
                case DataType.Boolean:
                    return br.ReadBoolean() ? DynValue.True : DynValue.False;
                case DataType.String:
                    return DynValue.NewString(br.ReadString());
                case DataType.Table:
                    int length = br.ReadInt32();
                    Table table = new Table(script);
                    for (int i = 0; i < length; i++)
                        table[ProcessItem()] = ProcessItem();
                    return DynValue.NewTable(table);
                case DataType.UserData:
                    byte b = br.ReadByte();
                    if (b == 1)
                    {
                        InstanceID instanceID = br.ReadUInt64();
                        Instance? instance = scriptContext.GameManager.GameRegistry.GetLocalInstanceById(instanceID);
                        if (instance == null)
                            return DynValue.Nil;
                        return DynValue.NewUserData(InstanceBridge.PushUserData(scriptContext.GameManager, instance));
                    }
                    else if (b == 2)
                    {
                        BrickColor brickColor = BrickColor.GetBrickColorByIndex(br.ReadInt32());
                        return DynValue.NewUserData(BrickColorBridge.PushUserData(scriptContext.GameManager, brickColor));
                    }
                    else if (b == 3)
                    {
                        Color3 color3 = default;
                        color3.R = br.ReadByte();
                        color3.G = br.ReadByte();
                        color3.B = br.ReadByte();
                        return DynValue.NewUserData(Color3Bridge.PushUserData(scriptContext.GameManager, color3));
                    }
                    else if (b == 4)
                    {
                        Type? enumtype = scriptContext.GameManager.GameRegistry.GetEnumFromRegisteredAssemblies(br.ReadString());
                        if (enumtype == null)
                            throw new InvalidOperationException("Cannot unpack unknown enum type!");

                        EnumValue enumValue = default;
                        enumValue.EnumType = enumtype;
                        enumValue.Value = br.ReadInt32();
                        enumValue.Name = Enum.GetName(enumtype, enumValue.Value) ?? "<network corruption>";
                        return DynValue.NewUserData(EnumValueBridge.PushUserData(scriptContext.GameManager, enumValue));
                    }
                    else if (b == 5)
                    {
                        UDim2 uDim2 = default;
                        uDim2.X.Scale = br.ReadSingle();
                        uDim2.X.Offset = br.ReadSingle();
                        uDim2.Y.Scale = br.ReadSingle();
                        uDim2.Y.Offset = br.ReadSingle();
                        return DynValue.NewUserData(UDim2Bridge.PushUserData(scriptContext.GameManager, uDim2));
                    }
                    else if (b == 6)
                    {
                        UDim uDim = default;
                        uDim.Scale = br.ReadSingle();
                        uDim.Offset = br.ReadSingle();
                        return DynValue.NewUserData(UDimBridge.PushUserData(scriptContext.GameManager, uDim));
                    }
                    else if (b == 7)
                    {
                        Vector2 vector2 = default;
                        vector2.X = br.ReadSingle();
                        vector2.Y = br.ReadSingle();
                        return DynValue.NewUserData(Vector2Bridge.PushUserData(scriptContext.GameManager, vector2));
                    }
                    else if (b == 8)
                    {
                        Vector3 vector3 = default;
                        vector3.X = br.ReadSingle();
                        vector3.Y = br.ReadSingle();
                        vector3.Z = br.ReadSingle();
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(scriptContext.GameManager, vector3));
                    }
                    break;
            }
            return DynValue.Nil;
        }

        return ProcessItem();
    }
    public static byte[] SerializeLuaToBytes(DynValue value)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        void ProcessItem(DynValue item)
        {
            bw.Write((byte)item.Type);
            switch (item.Type)
            {
                case DataType.Number:
                    bw.Write(item.Number);
                    break;
                case DataType.Boolean:
                    bw.Write(item.Boolean);
                    break;
                case DataType.String:
                    bw.Write(item.String);
                    break;
                case DataType.Table:
                    bw.Write(item.Table.Length);
                    foreach (TablePair tablePair in item.Table.Pairs)
                    {
                        ProcessItem(tablePair.Key);
                        ProcessItem(tablePair.Value);
                    }
                    break;
                case DataType.UserData:
                    IUserDataDescriptor userDataDescriptor = item.UserData.Descriptor;
                    if (userDataDescriptor is InstanceBridge.InstanceBridgeDescriptor inst)
                    {
                        bw.Write((byte)1);
                        bw.Write(inst.Value);
                    }
                    else if (userDataDescriptor is BrickColorBridge.BrickColorDescriptor brickColor)
                    {
                        bw.Write((byte)2);
                        bw.Write(brickColor.Value.Index);
                    }
                    else if (userDataDescriptor is Color3Bridge.Color3Descriptor color3)
                    {
                        bw.Write((byte)3);
                        bw.Write(color3.Value.R);
                        bw.Write(color3.Value.G);
                        bw.Write(color3.Value.B);
                    }
                    else if (userDataDescriptor is EnumValueBridge.EnumValueDescriptor enumval)
                    {
                        bw.Write((byte)4);
                        bw.Write(enumval.Value.EnumType.Name);
                        bw.Write(enumval.Value.Value);
                    }
                    else if (userDataDescriptor is UDim2Bridge.UDim2Descriptor udim2)
                    {
                        bw.Write((byte)5);
                        bw.Write(udim2.Value.X.Scale);
                        bw.Write(udim2.Value.X.Offset);
                        bw.Write(udim2.Value.Y.Scale);
                        bw.Write(udim2.Value.Y.Offset);
                    }
                    else if (userDataDescriptor is UDimBridge.UDimDescriptor udim)
                    {
                        bw.Write((byte)6);
                        bw.Write(udim.Value.Scale);
                        bw.Write(udim.Value.Offset);
                    }
                    else if (userDataDescriptor is Vector2Bridge.Vector2Descriptor vector2)
                    {
                        bw.Write((byte)7);
                        bw.Write(vector2.Value.X);
                        bw.Write(vector2.Value.Y);
                    }
                    else if (userDataDescriptor is Vector3Bridge.Vector3Descriptor vector3)
                    {
                        bw.Write((byte)8);
                        bw.Write(vector3.Value.X);
                        bw.Write(vector3.Value.Y);
                        bw.Write(vector3.Value.Z);
                    }
                    else
                    {
                        bw.Write((byte)9); // this means that there's nothing to serialize
                    }
                    break;
            }
        }

        ProcessItem(value);

        return ms.ToArray();
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
    private static readonly Type cachedLuaEventType = typeof(LuaEvent);
    private static readonly Type cachedLuaYieldType = typeof(LuaYield);
    private static readonly Type cachedLuaEventConnectionType = typeof(LuaEventConnection);
    private static readonly Type cachedDynValueType = typeof(DynValue);
}