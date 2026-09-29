using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;

namespace NetBlox.Runtime.Bridges;

public static class UDim2Bridge
{
    public class UDim2Descriptor : IUserDataDescriptor
    {
        public string Name => nameof(UDim2);
        public Type Type => typeof(UDim2);
        public UDim2 Value;
        public GameManager GameManager;

        public UDim2Descriptor(GameManager gm, UDim2 value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => Value.X + "," + Value.Y;
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "X":
                    case "Width":
                        return DynValue.NewUserData(UDimBridge.PushUserData(GameManager, Value.X));
                    case "Y":
                    case "Height":
                        return DynValue.NewUserData(UDimBridge.PushUserData(GameManager, Value.Y));
                }
            }

            throw new ScriptRuntimeException("No such property for UDim2: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "X":
                    case "Width":
                        if (value.Type != DataType.UserData || 
                            value.UserData.Descriptor is not UDimBridge.UDimDescriptor udimx)
                            throw new ScriptRuntimeException("X is supposed to be UDim");
                        Value.X = udimx.Value;
                        return true;
                    case "Y":
                    case "Height":
                        if (value.Type != DataType.UserData || 
                            value.UserData.Descriptor is not UDimBridge.UDimDescriptor udimy)
                            throw new ScriptRuntimeException("Y is supposed to be UDim");
                        Value.Y = udimy.Value;
                        return true;
                }
            }

            throw new ScriptRuntimeException("No such property for UDim2: " + index.ToString());
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
                    if (dynValue.UserData.Object is not UDim2Descriptor udimd)
                        throw new ScriptRuntimeException("Cannot add UDim2 to non-UDim2");
                    return DynValue.NewUserData(PushUserData(GameManager, Value + udimd.Value));
                });
            }
            else if (metaname == "__sub")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not UDim2Descriptor udimd)
                        throw new ScriptRuntimeException("Cannot subtract UDim2 from non-UDim2");
                    return DynValue.NewUserData(PushUserData(GameManager, Value - udimd.Value));
                });
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply UDim2");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide UDim2");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not UDim2Descriptor udimd)
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
        Table udim2module = scriptContext.AllocateTableInRegistry(nameof(UDim2Bridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table udim2meta = scriptContext.AllocateTableInRegistry(nameof(UDim2Bridge) + "_meta", out _);
            udim2meta["__index"] = udim2meta;
            udim2meta["__metatable"] = "Locked";
            udim2meta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 2)
                {
                    UserData x = args[0].CheckType("UDim2.new", DataType.UserData).UserData;
                    UserData y = args[1].CheckType("UDim2.new", DataType.UserData).UserData;
                    UDimBridge.UDimDescriptor xd = x.Descriptor as UDimBridge.UDimDescriptor ?? throw new Exception("Argument 1 must be UDim");
                    UDimBridge.UDimDescriptor yd = y.Descriptor as UDimBridge.UDimDescriptor ?? throw new Exception("Argument 2 must be UDim");
                    return DynValue.NewUserData(PushUserData(gameManager, new UDim2(xd.Value, yd.Value)));
                }
                else if (args.Count == 4)
                {
                    float xs = (float)args[0].CheckType("UDim2.new", DataType.Number).Number;
                    float xo = (float)args[1].CheckType("UDim2.new", DataType.Number).Number;
                    float ys = (float)args[2].CheckType("UDim2.new", DataType.Number).Number;
                    float yo = (float)args[3].CheckType("UDim2.new", DataType.Number).Number;
                    return DynValue.NewUserData(PushUserData(gameManager, new UDim2(xs, xo, ys, yo)));
                }
                throw new ScriptRuntimeException("UDim2.new requires 2 UDim arguments or 4 number arguments");
            });
            udim2module.MetaTable = udim2meta;
        }

        script.Globals["UDim2"] = udim2module;
    }
    public static UserData PushUserData(GameManager gameManager, UDim2 value)
    {
        var obj = new UDim2Descriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}