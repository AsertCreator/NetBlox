using System.Numerics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;

namespace NetBlox.Runtime.Bridges;

public static class Vector2Bridge
{
    public class Vector2Descriptor : IUserDataDescriptor
    {
        public string Name => nameof(Vector2);
        public Type Type => typeof(Vector2);
        public Vector2 Value;
        public GameManager GameManager;

        public Vector2Descriptor(GameManager gm, Vector2 value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => "<" + Value.X + " " + Value.Y + ">";
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
                    case "Magnitude":
                        return DynValue.NewNumber(Value.Length());
                }
            }

            throw new ScriptRuntimeException("No such property for Vector2: " + index.ToString());
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
                    case "Magnitude":
                        throw new ScriptRuntimeException("Magnitude is read-only");
                }
            }

            throw new ScriptRuntimeException("No such property for Vector2: " + index.ToString());
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
                    if (dynValue.UserData.Object is not Vector2Descriptor ve2d)
                        throw new ScriptRuntimeException("Cannot add Vector2 to non-Vector2");
                    return DynValue.NewUserData(PushUserData(GameManager, Value + ve2d.Value));
                });
            }
            else if (metaname == "__sub")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector2Descriptor ve2d)
                        throw new ScriptRuntimeException("Cannot subtract Vector2 from non-Vector2");
                    return DynValue.NewUserData(PushUserData(GameManager, Value - ve2d.Value));
                });
            }
            else if (metaname == "__mul")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.Type != DataType.UserData && dynValue.Type != DataType.Number)
                        throw new ScriptRuntimeException("Cannot multiply Vector2 to non-Vector2/number");
                    if (dynValue.Type == DataType.UserData && dynValue.UserData.Object is Vector2Descriptor ve2d)
                        return DynValue.NewUserData(PushUserData(GameManager, Value * ve2d.Value));
                    if (dynValue.Type == DataType.Number)
                        return DynValue.NewUserData(PushUserData(GameManager, Value * (float)dynValue.Number));
                    throw new ScriptRuntimeException("Cannot multiply Vector2 to non-Vector2/number");
                });
            }
            else if (metaname == "__div")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.Type != DataType.UserData && dynValue.Type != DataType.Number)
                        throw new ScriptRuntimeException("Cannot divide Vector2 by non-Vector2/number");
                    if (dynValue.Type == DataType.UserData && dynValue.UserData.Object is Vector2Descriptor ve2d)
                        return DynValue.NewUserData(PushUserData(GameManager, Value / ve2d.Value));
                    if (dynValue.Type == DataType.Number)
                        return DynValue.NewUserData(PushUserData(GameManager, Value / (float)dynValue.Number));
                    throw new ScriptRuntimeException("Cannot divide Vector2 by non-Vector2/number");
                });
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Vector2Descriptor other)
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
        Table vector2module = scriptContext.AllocateTableInRegistry(nameof(Vector2Bridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table vector2meta = scriptContext.AllocateTableInRegistry(nameof(Vector2Bridge) + "_meta", out _);
            vector2meta["__index"] = vector2meta;
            vector2meta["__metatable"] = "Locked";
            vector2meta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 2)
                {
                    float x = (float)args[0].CheckType("Vector2.new", DataType.Number).Number;
                    float y = (float)args[1].CheckType("Vector2.new", DataType.Number).Number;
                    return DynValue.NewUserData(PushUserData(gameManager, new Vector2(x, y)));
                }
                throw new ScriptRuntimeException("Vector2.new requires 2 number arguments");
            });
            vector2module.MetaTable = vector2meta;
        }

        script.Globals["Vector2"] = vector2module;
    }
    public static UserData PushUserData(GameManager gameManager, Vector2 value)
    {
        var obj = new Vector2Descriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}