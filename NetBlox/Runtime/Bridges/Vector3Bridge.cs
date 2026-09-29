using System.Numerics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;

namespace NetBlox.Runtime.Bridges;

public static class Vector3Bridge
{
    public class Vector3Descriptor : IUserDataDescriptor
    {
        public string Name => nameof(Vector3);
        public Type Type => typeof(Vector3);
        public Vector3 Value;
        public GameManager GameManager;

        public Vector3Descriptor(GameManager gm, Vector3 value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => "<" + Value.X + " " + Value.Y + " " + Value.Z + ">";
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "X":
                        return DynValue.NewNumber(Value.X);
                    case "Y":
                        return DynValue.NewNumber(Value.Y);
                    case "Z":
                        return DynValue.NewNumber(Value.Z);
                    case "Magnitude":
                        return DynValue.NewNumber(Value.Length());
                }
            }

            throw new ScriptRuntimeException("No such property for Vector3: " + index.ToString());
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
                        Value.X = (float)value.Number;
                        return true;
                    case "Y":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Y is supposed to be number");
                        Value.Y = (float)value.Number;
                        return true;
                    case "Z":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Z is supposed to be number");
                        Value.Z = (float)value.Number;
                        return true;
                    case "Magnitude":
                        throw new ScriptRuntimeException("Magnitude is read-only");
                }
            }

            throw new ScriptRuntimeException("No such property for Vector3: " + index.ToString());
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
                    if (dynValue.UserData.Object is not Vector3Descriptor ve3d)
                        throw new ScriptRuntimeException("Cannot add Vector3 to non-Vector3");
                    return DynValue.NewUserData(PushUserData(GameManager, Value + ve3d.Value));
                });
            }
            else if (metaname == "__sub")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector3Descriptor ve3d)
                        throw new ScriptRuntimeException("Cannot subtract Vector3 from non-Vector3");
                    return DynValue.NewUserData(PushUserData(GameManager, Value - ve3d.Value));
                });
            }
            else if (metaname == "__mul")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.Type != DataType.UserData && dynValue.Type != DataType.Number)
                        throw new ScriptRuntimeException("Cannot multiply Vector3 to non-Vector3/number");
                    if (dynValue.Type == DataType.UserData && dynValue.UserData.Object is Vector3Descriptor ve3d)
                        return DynValue.NewUserData(PushUserData(GameManager, Value * ve3d.Value));
                    if (dynValue.Type == DataType.Number)
                        return DynValue.NewUserData(PushUserData(GameManager, Value * (float)dynValue.Number));
                    throw new ScriptRuntimeException("Cannot multiply Vector3 to non-Vector3/number");
                });
            }
            else if (metaname == "__div")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.Type != DataType.UserData && dynValue.Type != DataType.Number)
                        throw new ScriptRuntimeException("Cannot divide Vector3 by non-Vector3/number");
                    if (dynValue.Type == DataType.UserData && dynValue.UserData.Object is Vector3Descriptor ve3d)
                        return DynValue.NewUserData(PushUserData(GameManager, Value / ve3d.Value));
                    if (dynValue.Type == DataType.Number)
                        return DynValue.NewUserData(PushUserData(GameManager, Value / (float)dynValue.Number));
                    throw new ScriptRuntimeException("Cannot divide Vector3 by non-Vector3/number");
                });
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector3Descriptor other)
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
        Table vector3module = scriptContext.AllocateTableInRegistry(nameof(Vector3Bridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table vector3meta = scriptContext.AllocateTableInRegistry(nameof(Vector3Bridge) + "_meta", out _);
            vector3meta["__index"] = vector3meta;
            vector3meta["__metatable"] = "Locked";
            vector3meta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 3)
                {
                    float x = (float)args[0].CheckType("Vector3.new", DataType.Number).Number;
                    float y = (float)args[1].CheckType("Vector3.new", DataType.Number).Number;
                    float z = (float)args[2].CheckType("Vector3.new", DataType.Number).Number;
                    return DynValue.NewUserData(PushUserData(gameManager, new Vector3(x, y, z)));
                }
                throw new ScriptRuntimeException("Vector3.new requires 3 number arguments");
            });
            vector3module.MetaTable = vector3meta;
        }

        script.Globals["Vector3"] = vector3module;
    }
    public static UserData PushUserData(GameManager gameManager, Vector3 value)
    {
        var obj = new Vector3Descriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}