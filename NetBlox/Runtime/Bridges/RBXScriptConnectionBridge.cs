using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime.Bridges;

public static class RBXScriptConnectionBridge
{
    public class RBXScriptConnectionDescriptor : IUserDataDescriptor
    {
        public string Name => "RBXScriptConnection";
        public Type Type => typeof(LuaEventConnection);
        public LuaEventConnection Value;
        public GameManager GameManager;

        public RBXScriptConnectionDescriptor(GameManager gm, LuaEventConnection value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => "RBXScriptConnection";
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Disconnect":
                        return DynValue.NewCallback((x, y) =>
                        {
                            if (!Value.LuaEvent.Disconnect(Value))
                                throw new InvalidOperationException("This RBXScriptConnection is already disconnected");
                            return DynValue.Void; 
                        });
                }
            }

            throw new ScriptRuntimeException("No such method for RBXScriptConnection: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("RBXScriptConnection is readonly");
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add RBXScriptConnection");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract RBXScriptConnection");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply RBXScriptConnection");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide RBXScriptConnection");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not RBXScriptConnectionDescriptor rssd)
                        return DynValue.False;
                    if (rssd.Value == Value)
                        return DynValue.True;
                    return DynValue.False;
                });
            }
            return DynValue.Nil;
        }
    }

    public static void Setup(GameManager gameManager)
    {
        // no
    }
    public static UserData PushUserData(GameManager gameManager, LuaEventConnection value)
    {
        var obj = new RBXScriptConnectionDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}