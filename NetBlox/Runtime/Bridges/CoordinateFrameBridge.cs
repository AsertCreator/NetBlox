using System.Numerics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Runtime.Bridges;

public static class CoordinateFrameBridge
{
    public class CoordinateFrameDescriptor : IUserDataDescriptor
    {
        public string Name => nameof(CoordinateFrame);
        public Type Type => typeof(CoordinateFrame);
        public CoordinateFrame Value;
        public GameManager GameManager;

        public CoordinateFrameDescriptor(GameManager gm, CoordinateFrame value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => "<" + Value.Position.X + " " + Value.Position.Y + " " + Value.Position.Z + " (cframe)>";
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "X":
                        return DynValue.NewNumber(Value.Position.X);
                    case "Y":
                        return DynValue.NewNumber(Value.Position.Y);
                    case "Z":
                        return DynValue.NewNumber(Value.Position.Z);
                    case "RightVector":
                    case "XVector":
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, 
                            new Vector3(Value.Orientation.M11, Value.Orientation.M21, Value.Orientation.M31)));
                    case "YVector":
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, 
                            new Vector3(Value.Orientation.M12, Value.Orientation.M22, Value.Orientation.M32)));
                    case "ZVector":
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, 
                            -new Vector3(Value.Orientation.M13, Value.Orientation.M23, Value.Orientation.M33)));
                    case "LookVector":
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, 
                            new Vector3(Value.Orientation.M13, Value.Orientation.M23, Value.Orientation.M33)));
                    case "Position":
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, Value.Position));
                    case "Rotation":
                        return DynValue.NewUserData(PushUserData(GameManager, new CoordinateFrame()
                        {
                            Position = Value.Position
                        }));
                }
            }

            throw new ScriptRuntimeException("No such property for CFrame: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "X":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("X is supposed to be number");
                        Value.Position.X = (float)value.Number;
                        return true;
                    case "Y":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Y is supposed to be number");
                        Value.Position.Y = (float)value.Number;
                        return true;
                    case "Z":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Z is supposed to be number");
                        Value.Position.Z = (float)value.Number;
                        return true;
                }
            }

            throw new ScriptRuntimeException("No such property for CFrame: " + index.ToString());
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector3Bridge.Vector3Descriptor ve3d)
                        throw new ScriptRuntimeException("Cannot add CFrame to non-Vector3");
                    CoordinateFrame coordinateFrame = Value;
                    coordinateFrame.Position += ve3d.Value;
                    return DynValue.NewUserData(PushUserData(GameManager, coordinateFrame));
                });
            }
            else if (metaname == "__sub")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector3Bridge.Vector3Descriptor ve3d)
                        throw new ScriptRuntimeException("Cannot subtract CFrame from non-Vector3");
                    CoordinateFrame coordinateFrame = Value;
                    coordinateFrame.Position += ve3d.Value;
                    return DynValue.NewUserData(PushUserData(GameManager, coordinateFrame));
                });
            }
            else if (metaname == "__mul")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is Vector3Bridge.Vector3Descriptor ve3d)
                    {
                        Quaternion quaternion = Raymath.QuaternionFromMatrix(Value.Orientation);
                        Vector3 world = Raymath.Vector3RotateByQuaternion(ve3d.Value, quaternion) + Value.Position;
                        return DynValue.NewUserData(Vector3Bridge.PushUserData(GameManager, world));
                    }
                    else if (dynValue.UserData.Object is CoordinateFrameDescriptor cframed)
                    {
                        Quaternion quaternion = Raymath.QuaternionFromMatrix(Value.Orientation);
                        Vector3 world = Raymath.Vector3RotateByQuaternion(cframed.Value.Position, quaternion) + Value.Position;
                        Matrix4x4 rotation = Value.Orientation * cframed.Value.Orientation;
                        CoordinateFrame coordinateFrame = default;
                        
                        coordinateFrame.Orientation = rotation;
                        coordinateFrame.Position = world;

                        return DynValue.NewUserData(PushUserData(GameManager, coordinateFrame));
                    }
                    throw new ScriptRuntimeException("Cannot apply CFrame to non-Vector3/CFrame");
                });
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide CFrame");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not CoordinateFrameDescriptor other)
                        return DynValue.False;
                    if (other.Value == Value)
                        return DynValue.True;
                    return DynValue.False;
                });
            }
            return DynValue.Nil;
        }
    }

    public static void Setup(GameManager gameManager)
    {
        ScriptContext scriptContext = gameManager.RootModel.GetService<ScriptContext>();
        SecurityIdentity securityIdentity = gameManager.GameScheduler.GetCurrentSecurityIdentity()!;
        Table CoordinateFramemodule = scriptContext.AllocateTableInRegistry(nameof(CoordinateFrameBridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table CoordinateFramemeta = scriptContext.AllocateTableInRegistry(nameof(CoordinateFrameBridge) + "_meta", out _);
            CoordinateFramemeta.Frozen = false;
            CoordinateFramemeta["__index"] = CoordinateFramemeta;
            CoordinateFramemeta["__metatable"] = "Locked";
            CoordinateFramemeta["identity"] = DynValue.NewUserData(PushUserData(gameManager, new CoordinateFrame()
            {
                Position = default,
                Orientation = Matrix4x4.Identity
            }));
            CoordinateFramemeta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, default));
                }
                else if (args.Count == 1)
                {
                    UserData userData = args[0].CheckType("CFrame.new", DataType.UserData).UserData;

                    if (userData.Descriptor is Vector3Bridge.Vector3Descriptor desc)
                    {
                        CoordinateFrame coordinateFrame = default;
                        coordinateFrame.Position = desc.Value;
                        coordinateFrame.Orientation = Matrix4x4.Identity;
                        return DynValue.NewUserData(PushUserData(gameManager, coordinateFrame));
                    }
                }
                else if (args.Count == 2)
                {
                    UserData userData1 = args[0].CheckType("CFrame.new", DataType.UserData).UserData;
                    UserData userData2 = args[1].CheckType("CFrame.new", DataType.UserData).UserData;

                    if (userData1.Descriptor is Vector3Bridge.Vector3Descriptor desc1 && userData2.Descriptor is Vector3Bridge.Vector3Descriptor desc2)
                    {
                        CoordinateFrame coordinateFrame = default;
                        coordinateFrame.Position = desc1.Value;
                        coordinateFrame.Orientation = Raymath.MatrixLookAt(desc1.Value, desc2.Value, Vector3.UnitY);
                        return DynValue.NewUserData(PushUserData(gameManager, coordinateFrame));
                    }
                }
                else if (args.Count == 3)
                {
                    float x = (float)args[0].CheckType("CFrame.new", DataType.Number).Number;
                    float y = (float)args[1].CheckType("CFrame.new", DataType.Number).Number;
                    float z = (float)args[2].CheckType("CFrame.new", DataType.Number).Number;

                    CoordinateFrame coordinateFrame = default;
                    coordinateFrame.Position.X = x;
                    coordinateFrame.Position.Y = y;
                    coordinateFrame.Position.Z = z;
                    coordinateFrame.Orientation = Matrix4x4.Identity;

                    return DynValue.NewUserData(PushUserData(gameManager, coordinateFrame));
                }
                else if (args.Count == 7)
                {
                    float x = (float)args[0].CheckType("CFrame.new", DataType.Number).Number;
                    float y = (float)args[1].CheckType("CFrame.new", DataType.Number).Number;
                    float z = (float)args[2].CheckType("CFrame.new", DataType.Number).Number;
                    float qx = (float)args[3].CheckType("CFrame.new", DataType.Number).Number;
                    float qy = (float)args[4].CheckType("CFrame.new", DataType.Number).Number;
                    float qz = (float)args[5].CheckType("CFrame.new", DataType.Number).Number;
                    float qw = (float)args[6].CheckType("CFrame.new", DataType.Number).Number;

                    Quaternion quaternion = default;
                    quaternion.W = qw;
                    quaternion.X = qx;
                    quaternion.Y = qy;
                    quaternion.Z = qz;

                    CoordinateFrame coordinateFrame = default;
                    coordinateFrame.Position.X = x;
                    coordinateFrame.Position.Y = y;
                    coordinateFrame.Position.Z = z;
                    coordinateFrame.Orientation = Raymath.QuaternionToMatrix(quaternion);

                    return DynValue.NewUserData(PushUserData(gameManager, coordinateFrame));
                }
                else if (args.Count == 12)
                {
                    float x = (float)args[0].CheckType("CFrame.new", DataType.Number).Number;
                    float y = (float)args[1].CheckType("CFrame.new", DataType.Number).Number;
                    float z = (float)args[2].CheckType("CFrame.new", DataType.Number).Number;
                    float r00 = (float)args[3].CheckType("CFrame.new", DataType.Number).Number;
                    float r01 = (float)args[4].CheckType("CFrame.new", DataType.Number).Number;
                    float r02 = (float)args[5].CheckType("CFrame.new", DataType.Number).Number;
                    float r10 = (float)args[6].CheckType("CFrame.new", DataType.Number).Number;
                    float r11 = (float)args[7].CheckType("CFrame.new", DataType.Number).Number;
                    float r12 = (float)args[8].CheckType("CFrame.new", DataType.Number).Number;
                    float r20 = (float)args[9].CheckType("CFrame.new", DataType.Number).Number;
                    float r21 = (float)args[10].CheckType("CFrame.new", DataType.Number).Number;
                    float r22 = (float)args[11].CheckType("CFrame.new", DataType.Number).Number;

                    CoordinateFrame coordinateFrame = default;
                    coordinateFrame.Position.X = x;
                    coordinateFrame.Position.Y = y;
                    coordinateFrame.Position.Z = z;
                    coordinateFrame.Orientation.M11 = r00;
                    coordinateFrame.Orientation.M12 = r01;
                    coordinateFrame.Orientation.M13 = r02;
                    coordinateFrame.Orientation.M21 = r10;
                    coordinateFrame.Orientation.M22 = r11;
                    coordinateFrame.Orientation.M23 = r12;
                    coordinateFrame.Orientation.M31 = r20;
                    coordinateFrame.Orientation.M32 = r21;
                    coordinateFrame.Orientation.M33 = r22;

                    return DynValue.NewUserData(PushUserData(gameManager, coordinateFrame));
                }

                throw new ScriptRuntimeException("CFrame.new requires 0 or 1 Vector3 or 3 number or 7 number arguments");
            });
            CoordinateFramemodule.Frozen = false;
            CoordinateFramemodule.MetaTable = CoordinateFramemeta;

            CoordinateFramemeta.Frozen = true;
            CoordinateFramemodule.Frozen = true;
        }

        var ogf = script.Globals.Frozen;
        script.Globals.Frozen = false;
        script.Globals["CFrame"] = CoordinateFramemodule;
        script.Globals.Frozen = ogf;
    }
    public static UserData PushUserData(GameManager gameManager, CoordinateFrame value)
    {
        var obj = new CoordinateFrameDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}