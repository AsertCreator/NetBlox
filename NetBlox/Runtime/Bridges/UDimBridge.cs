using System.Numerics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;

namespace NetBlox.Runtime.Bridges;

public static class UDimBridge
{
    public class UDimDescriptor : IUserDataDescriptor
    {
        public string Name => nameof(UDim);
        public Type Type => typeof(UDim);
        public UDim Value;
        public GameManager GameManager;

        public UDimDescriptor(GameManager gm, UDim value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => Value.Scale + "," + Value.Offset;
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Scale":
                        return DynValue.NewNumber(Value.Scale);
                    case "Offset":
                        return DynValue.NewNumber(Value.Offset);
                }
            }

            throw new ScriptRuntimeException("No such property for UDim: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Scale":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Scale is supposed to be number");
                        Value.Scale = (float)value.Number;
                        return true;
                    case "Offset":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("Offset is supposed to be number");
                        Value.Offset = (float)value.Number;
                        return true;
                }
            }

            throw new ScriptRuntimeException("No such property for UDim: " + index.ToString());
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
                    if (dynValue.UserData.Object is not UDimDescriptor udimd)
                        throw new ScriptRuntimeException("Cannot add UDim to non-UDim");
                    return DynValue.NewUserData(PushUserData(GameManager, Value + udimd.Value));
                });
            }
            else if (metaname == "__sub")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not UDimDescriptor udimd)
                        throw new ScriptRuntimeException("Cannot subtract UDim from non-UDim");
                    return DynValue.NewUserData(PushUserData(GameManager, Value - udimd.Value));
                });
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply UDim");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide UDim");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not UDimDescriptor udimd)
                        return DynValue.False;
                    if (udimd.Value == Value)
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
        Table udimmodule = scriptContext.AllocateTableInRegistry(nameof(UDimBridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table udimmeta = scriptContext.AllocateTableInRegistry(nameof(UDimBridge) + "_meta", out _);
            udimmeta["__index"] = udimmeta;
            udimmeta["__metatable"] = "Locked";
            udimmeta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 2)
                {
                    float scale = (float)args[0].CheckType("UDim.new", DataType.Number).Number;
                    float offset = (float)args[1].CheckType("UDim.new", DataType.Number).Number;
                    return DynValue.NewUserData(PushUserData(gameManager, new UDim(scale, offset)));
                }
                throw new ScriptRuntimeException("UDim.new requires 2 number arguments");
            });
            udimmodule.MetaTable = udimmeta;
        }

        script.Globals["UDim"] = udimmodule;
    }
    public static UserData PushUserData(GameManager gameManager, UDim value)
    {
        var obj = new UDimDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}