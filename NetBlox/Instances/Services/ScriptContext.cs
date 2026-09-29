using System.Diagnostics;
using NetBlox.Instances.Scripts;
using NetBlox.Runtime;
using MoonSharp.Interpreter;

using Script = MoonSharp.Interpreter.Script;
using System.Text;
using NetBlox.Runtime.Bridges;
using NetBlox.Testing;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
public class ScriptContext : Instance
{
    public override string ClassName => nameof(ScriptContext);

    public const ulong NETWORK_CONSTANT_ID = 11;

    public Dictionary<SecurityIdentity, Dictionary<string, Table>> Registry = [];
    public Dictionary<Type, InstanceBridge.InstanceClassCache> InstanceClassCache = [];

    private Dictionary<SecurityIdentity, Dictionary<ulong, DynValue>> moduleCache = [];
    private Dictionary<SecurityIdentity, Script> luaStates = [];

    public ScriptContext(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        Name = "Script Context";
    }
    public Table AllocateTableInRegistry(string id, out bool newTable)
    {
        SecurityIdentity identity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;
        Debug.Assert(identity != null);

        newTable = false;
        
        if (!Registry.ContainsKey(identity))
            Registry[identity] = new Dictionary<string, Table>();
        if (!Registry[identity].ContainsKey(id))
        {
            Registry[identity][id] = new Table(GetLuaStateFor(identity));
            newTable = true;
        }
        
        return Registry[identity][id];
    }
    public Script GetLuaStateFor(SecurityIdentity securityIdentity)
    {
        if (luaStates.TryGetValue(securityIdentity, out Script? luaState))
            return luaState;
        
        luaState = new Script(CoreModules.Metatables | CoreModules.String | CoreModules.Table | CoreModules.OS_System | CoreModules.Basic |
            CoreModules.Math | CoreModules.TableIterators | CoreModules.Coroutine | CoreModules.Bit32 | CoreModules.OS_Time);

        luaStates[securityIdentity] = luaState;

        luaState.Globals["_G"] = DynValue.NewTable(luaState);
        luaState.Globals["shared"] = DynValue.NewTable(luaState);
        luaState.Globals["getgenv"] = DynValue.Nil;
        luaState.Globals["setgenv"] = DynValue.Nil;

        luaState.Globals["print"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].IsVoid())
					break;

				if (i != 0)
					sb.Append('\t');

				sb.Append(args.AsStringUsingMeta(executionContext, i, "print"));
			}

            PrintOut(sb.ToString());

            return DynValue.Nil;
        });

        luaState.Globals["warn"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].IsVoid())
					break;

				if (i != 0)
					sb.Append('\t');

				sb.Append(args.AsStringUsingMeta(executionContext, i, "warn"));
			}

            PrintOutWarning(sb.ToString());

            return DynValue.Nil;
        });

        luaState.Globals["printidentity"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
            SecurityIdentity identity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;
            Debug.Assert(identity != null);
            PrintOut("Current identity is " + identity.SecurityIdentityNumber);
            return DynValue.Nil;
        });

        luaState.Globals["loadstring"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (GameManager.NetworkMode == Network.NetworkMode.Client ||
                !GameManager.RootModel.GetService<ServerScriptService>().LoadStringEnabled)
                throw new InvalidOperationException("loadstring() is not available");
            string chunk = args[0].CheckType("loadstring", DataType.String).String;
            return executionContext.OwnerScript.LoadString(chunk);
        });

        luaState.Globals["require"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
            SecurityIdentity identity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;
            Debug.Assert(identity != null);

            if (args.Count == 1)
            {
                if (args[0].Type == DataType.UserData && 
                    args[0].UserData.Descriptor is InstanceBridge.InstanceBridgeDescriptor instbd &&
                    GameManager.GameRegistry.TryResolveInstanceById(instbd.Value, out Instance? instance) &&
                    instance is ModuleScript ms)
                {
                    if (!moduleCache.TryGetValue(identity, out Dictionary<ulong, DynValue>? cache))
                    {
                        cache = new Dictionary<ulong, DynValue>();
                        moduleCache[identity] = cache;
                    }

                    if (cache.TryGetValue(ms.InstanceID, out DynValue? moduleScriptCache))
                        return moduleScriptCache;

                    GameSchedulerTask? task = StartScriptWithCurrentIdentity(ms, true);
                    GameManager.GameScheduler.CurrentSchedulerTask!.WaitingTask = task;
                    GameManager.GameScheduler.CurrentSchedulerTask!.WaitingTaskHandler = HandleModuleScriptExecutionCompletion;
                    task!.TasksYouShouldPutAfterMe = [GameManager.GameScheduler.CurrentSchedulerTask];
                    return DynValue.NewYieldReq([]);
                }
                else if (args[0].Type == DataType.Number)
                {
                    long assetId = (long)args[0].Number;

                    throw new NotImplementedException("not yet");
                }
            }
            
            throw new ArgumentException("require() requires 1 ModuleScript argument or 1 number argument");
        });

        luaState.Globals["wait"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
        {
            GameSchedulerTask? task = GameManager.GameScheduler.CurrentSchedulerTask;
            Debug.Assert(task != null);

            if (args.Count == 1 && args[0].Type == DataType.Number)
            {
                double waitTime = args[0].Number;

                task.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromSeconds(waitTime));

                return DynValue.NewYieldReq([]);
            }
            
            throw new ArgumentException("wait() requires 1 number argument");
        });

        luaState.Globals["game"] = DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, Root));
        luaState.Globals["Game"] = DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, Root));
        luaState.Globals["workspace"] = DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, Root.GetService<Workspace>()));
        luaState.Globals["Workspace"] = DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, Root.GetService<Workspace>()));

        Vector3Bridge.Setup(GameManager);
        Vector2Bridge.Setup(GameManager);
        Color3Bridge.Setup(GameManager);
        UDimBridge.Setup(GameManager);
        UDim2Bridge.Setup(GameManager);
        BrickColorBridge.Setup(GameManager);
        InstanceBridge.Setup(GameManager);
        EnumValueBridge.Setup(GameManager);

        return luaState;
    }
    public InstanceBridge.InstanceClassCache ResolveInstanceClassCacheForType(Type type)
    {
        if (InstanceClassCache.TryGetValue(type, out InstanceBridge.InstanceClassCache? cache))
            return cache;
        cache = InstanceBridge.InstanceClassCache.Construct(this, type);
        InstanceClassCache[type] = cache;
        return cache;
    }
    public void PrintOut(string info)
    {
        Trace.TraceInformation("\"" + info + "\"");
        GameManager.TryGetEventForId(GameEvent.EVENT_LOG_INFO)?.Fire(info);
    }
    public void PrintOutWarning(string info)
    {
        Trace.TraceWarning("\"" + info + "\"");
        GameManager.TryGetEventForId(GameEvent.EVENT_LOG_WARN)?.Fire(info);
    }
    public GameSchedulerTask? StartScriptWithCurrentIdentity(BaseScript baseScript, bool doNotDefer = false)
    {
        if (!SecurityIdentity.AllowedSecurityIdentites.Contains(GameManager.GameScheduler.GetCurrentSecurityIdentity()))
        {
            Trace.TraceError("Current security identity cannot start scripts!");
            return null;
        }

        var schedulerTask = new ScriptSchedulerTask(baseScript, baseScript.Source)
        {
            DebugName = baseScript.GetFullName(),
            GameScheduler = GameManager.GameScheduler,
            Identity = GameManager.GameScheduler.GetCurrentSecurityIdentity(),
            Delegate = _ => SchedulerTaskResult.NotCompleted
        };

        if (doNotDefer)
            return GameManager.GameScheduler.ScheduleForImmediateExecution(schedulerTask);
        else
            return GameManager.GameScheduler.Schedule(schedulerTask);
    }
    public GameSchedulerTask? StartSourceScriptWithCurrentIdentity(string name, string source, bool doNotDefer = false)
    {
        if (!SecurityIdentity.AllowedSecurityIdentites.Contains(GameManager.GameScheduler.GetCurrentSecurityIdentity()))
        {
            Trace.TraceError("Current security identity cannot start scripts!");
            return null;
        }

        var schedulerTask = new ScriptSchedulerTask(null, source)
        {
            DebugName = name,
            GameScheduler = GameManager.GameScheduler,
            Identity = GameManager.GameScheduler.GetCurrentSecurityIdentity(),
            Delegate = _ => SchedulerTaskResult.NotCompleted
        };

        if (doNotDefer)
            return GameManager.GameScheduler.ScheduleForImmediateExecution(schedulerTask);
        else
            return GameManager.GameScheduler.Schedule(schedulerTask);
    }
    public GameSchedulerTask StartTestingScriptWithCurrentIdentity(BaseTest test, string content, bool doNotDefer = false)
    {
        if (!SecurityIdentity.AllowedSecurityIdentites.Contains(GameManager.GameScheduler.GetCurrentSecurityIdentity()))
        {
            Trace.TraceError("StartTestingScriptWithCurrentIdentity: Current security identity cannot start scripts!");
            return null!;
        }

        var schedulerTask = new ScriptSchedulerTask(null, content)
        {
            DebugName = "Test - " + test,
            GameScheduler = GameManager.GameScheduler,
            Identity = GameManager.GameScheduler.GetCurrentSecurityIdentity(),
            Delegate = _ => SchedulerTaskResult.NotCompleted,

            CallbackAfterSuccessfulExecution = () =>
            {
                test.ReportSuccess();
            },
            CallbackAfterFailedExecution = () =>
            {
                test.ReportFailure_ScriptingFailure();
            }
        };

        if (doNotDefer)
            return GameManager.GameScheduler.ScheduleForImmediateExecution(schedulerTask);
        else
            return GameManager.GameScheduler.Schedule(schedulerTask);
    }
    public Table CreateNewEphemeralLocalEnvironment()
    {
        SecurityIdentity securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;
        Debug.Assert(securityIdentity != null);
        Table table = new Table(GetLuaStateFor(securityIdentity));

        table.MetaTable = new Table(GetLuaStateFor(securityIdentity));
        table.MetaTable["__index"] = GetLuaStateFor(securityIdentity).Globals;
        table.MetaTable["__metatable"] = "Locked";

        return table;
    }
    public Table CreateNewLocalEnvironment(BaseScript baseScript)
    {
        SecurityIdentity securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;
        Debug.Assert(securityIdentity != null);
        Table table = new Table(GetLuaStateFor(securityIdentity));

        table["script"] = baseScript;
        table.MetaTable = new Table(GetLuaStateFor(securityIdentity));
        table.MetaTable["__index"] = GetLuaStateFor(securityIdentity).Globals;
        table.MetaTable["__metatable"] = "Locked";

        return table;
    }

    private void HandleModuleScriptExecutionCompletion(GameSchedulerTask waiter, GameSchedulerTask waitee)
    {
        if (waitee.Result == SchedulerTaskResult.CompletedSuccess)
        {
            ScriptSchedulerTask? scriptSchedulerTask1 = waiter as ScriptSchedulerTask;
            ScriptSchedulerTask? scriptSchedulerTask2 = waitee as ScriptSchedulerTask;
            Debug.Assert(scriptSchedulerTask1 != null);
            Debug.Assert(scriptSchedulerTask2 != null);

            ModuleScript? moduleScript = scriptSchedulerTask2.Self as ModuleScript;
            Debug.Assert(moduleScript != null);

            SecurityIdentity? securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity();
            Debug.Assert(securityIdentity != null);

            moduleCache[securityIdentity].Add(moduleScript.InstanceID, scriptSchedulerTask2.AsyncFunctionArguments);

            scriptSchedulerTask1.AsyncFunctionArguments = scriptSchedulerTask2.AsyncFunctionArguments;
        }
        else
        {
            throw new Exception("While loading a ModuleScript, an error occurred");
        }
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ScriptContext))
            return base.IsA(className);
        return true;
    }
}