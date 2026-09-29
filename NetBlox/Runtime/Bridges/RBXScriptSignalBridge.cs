using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Scripts;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime.Bridges;

public static class RBXScriptSignalBridge
{
    public class RBXScriptSignalDescriptor : IUserDataDescriptor
    {
        public string Name => "RBXScriptSignal";
        public Type Type => typeof(LuaEvent);
        public LuaEvent Value;
        public GameManager GameManager;

        public RBXScriptSignalDescriptor(GameManager gm, LuaEvent value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => "RBXScriptSignal";
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Connect":
                        return DynValue.NewCallback((x, y) =>
                        {
                            GameSchedulerTask? task = GameManager.GameScheduler.CurrentSchedulerTask;
                            BaseScript? attacher = null;
                            if (task is ScriptSchedulerTask sst)
                                attacher = sst.Self;
                            LuaEventConnection? eventConnection = Value.ConnectWithCurrentSecurityIdentity(GameManager, y[1], attacher);
                            if (eventConnection == null)
                                throw new Exception("Failed to connect to an event");
                            return DynValue.Void; 
                        });
                    case "Wait":
                        return DynValue.NewCallback((x, y) =>
                        {
                            Value.AddAwaitingTask(GameManager.GameScheduler.CurrentSchedulerTask!);
                            return DynValue.NewYieldReq([]);
                        });
                }
            }

            throw new ScriptRuntimeException("No such method for RBXScriptSignal: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("RBXScriptSignal is readonly");
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add RBXScriptSignal");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract RBXScriptSignal");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply RBXScriptSignal");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide RBXScriptSignal");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not RBXScriptSignalDescriptor rssd)
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
    public static UserData PushUserData(GameManager gameManager, LuaEvent value)
    {
        var obj = new RBXScriptSignalDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}