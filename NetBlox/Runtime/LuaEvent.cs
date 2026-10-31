using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;

namespace NetBlox.Runtime;

// this implementation is very fragile. i can see how it can break just from passing tables or functions
// to the event arguments. thankfully that doesn't happen in roblox or netblox-exclusive code. yet.

public record LuaEventConnection(LuaEvent LuaEvent, SecurityIdentity Identity, GameManager GameManager, DynValue Handler, BaseScript? Attacher);

public class LuaEvent
{
    public event EventHandler<DynValue[]>? OnBeforeFired;
    public event EventHandler<DynValue[]>? OnAfterFired;

    public bool HasConnections => eventListeners.Count > 0;
    public bool HasWaiters => awaitingTasks.Count > 0;

    private List<LuaEventConnection> eventListeners;
    private List<GameSchedulerTask> awaitingTasks;

    public LuaEvent()
    {
        eventListeners = new List<LuaEventConnection>();
        awaitingTasks = new List<GameSchedulerTask>();
    }

    public LuaEventConnection[] GetEventListeners() => eventListeners.ToArray();
    public GameSchedulerTask[] GetAwaitingTasks() => awaitingTasks.ToArray();

    public LuaEventConnection? ConnectWithCurrentSecurityIdentity(GameManager gameManager, DynValue dynValue, BaseScript? attacher)
    {
        SecurityIdentity? securityIdentity = gameManager.GameScheduler.GetCurrentSecurityIdentity();
        if (securityIdentity == null)
            return null;
        LuaEventConnection eventConnection = new LuaEventConnection(this, securityIdentity, gameManager, dynValue, attacher);
        eventListeners.Add(eventConnection);
        return eventConnection;
    }
    public LuaEventConnection? Connect(GameManager gameManager, SecurityIdentity securityIdentity, DynValue dynValue, BaseScript? attacher)
    {
        LuaEventConnection eventConnection = new LuaEventConnection(this, securityIdentity, gameManager, dynValue, attacher);
        eventListeners.Add(eventConnection);
        return eventConnection;
    }
    public void FireWithNetworkLuaPayload(byte[] payload)
    {
        Dictionary<SecurityIdentity, DynValue> cached = [];

        DynValue DecodeFor(GameManager gameManager, SecurityIdentity securityIdentity)
        {
            DynValue dynValue = LuaMarshal.DeserializeLuaFromBytes(payload, gameManager.RootModel.GetService<ScriptContext>(), securityIdentity);
            List<DynValue> arguments = [];

            if (dynValue.Type == DataType.Table)
                arguments.AddRange(dynValue.Table.Values.ToArray());
            else if (dynValue.Type == DataType.Tuple)
                arguments.AddRange(dynValue.Tuple);
            
            return DynValue.NewTuple(arguments.ToArray());
        }

        for (int i = 0; i < eventListeners.Count; i++)
        {
            LuaEventConnection eventConnection = eventListeners[i];
            GameManager gameManager = eventConnection.GameManager;
            BaseScript? attacher = eventConnection.Attacher;
            DynValue function = eventConnection.Handler;

            ScriptSchedulerTask task = (ScriptSchedulerTask)gameManager.GameScheduler.Schedule(new ScriptSchedulerTask(attacher, function)
            {
                DebugName = $"Event Handler - {attacher?.ToString() ?? "<>"} - {function}",
                GameScheduler = gameManager.GameScheduler,
                Delegate = _ => SchedulerTaskResult.NotCompleted
            });

            if (!cached.ContainsKey(eventConnection.Identity))
                cached[eventConnection.Identity] = DecodeFor(gameManager, eventConnection.Identity);

            task.Identity = eventConnection.Identity;
            task.AsyncFunctionArguments = cached[eventConnection.Identity];
        }

        if (awaitingTasks.Count > 0)
        {
            for (int i = 0; i < awaitingTasks.Count; i++)
            {
                if (awaitingTasks[i] is ScriptSchedulerTask scriptSchedulerTask)
                {
                    if (!cached.ContainsKey(scriptSchedulerTask.Identity!))
                        cached[scriptSchedulerTask.Identity!] = DecodeFor(scriptSchedulerTask.GameScheduler.GameManager, scriptSchedulerTask.Identity!);
                    scriptSchedulerTask.AsyncFunctionArguments = cached[scriptSchedulerTask.Identity!];
                }
                awaitingTasks[i].IsWaiting = false;
            }
            awaitingTasks.Clear();
        }
    }
    public void Fire(DynValue[] args)
    {
        OnBeforeFired?.Invoke(this, args);

        DynValue tuple = DynValue.NewTuple(args);

        for (int i = 0; i < eventListeners.Count; i++)
        {
            LuaEventConnection eventConnection = eventListeners[i];
            GameManager gameManager = eventConnection.GameManager;
            BaseScript? attacher = eventConnection.Attacher;
            DynValue function = eventConnection.Handler;

            ScriptSchedulerTask task = (ScriptSchedulerTask)gameManager.GameScheduler.Schedule(new ScriptSchedulerTask(attacher, function)
            {
                DebugName = $"Event Handler - {attacher?.ToString() ?? "<>"} - {function}",
                GameScheduler = gameManager.GameScheduler,
                Delegate = _ => SchedulerTaskResult.NotCompleted
            });

            task.Identity = eventConnection.Identity;
            task.AsyncFunctionArguments = tuple; // literally how did i not notice that
        }

        if (awaitingTasks.Count > 0)
        {
            for (int i = 0; i < awaitingTasks.Count; i++)
            {
                if (awaitingTasks[i] is ScriptSchedulerTask scriptSchedulerTask)
                    scriptSchedulerTask.AsyncFunctionArguments = tuple;
                awaitingTasks[i].IsWaiting = false;
            }
            awaitingTasks.Clear();
        }

        OnAfterFired?.Invoke(this, args);
    }
    public bool Disconnect(LuaEventConnection eventConnection)
    {
        return eventListeners.Remove(eventConnection);
    }
    public void AddAwaitingTask(GameSchedulerTask task)
    {
        awaitingTasks.Add(task);
        task.IsWaiting = true;
    }
}