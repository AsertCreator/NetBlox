using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;

namespace NetBlox.Runtime;

public record LuaEventConnection(LuaEvent LuaEvent, SecurityIdentity Identity, GameManager GameManager, DynValue Handler, BaseScript? Attacher);

public class LuaEvent
{
    public event EventHandler<DynValue[]>? OnBeforeFired;
    public event EventHandler<DynValue[]>? OnAfterFired;

    private List<LuaEventConnection> eventListeners;
    private List<GameSchedulerTask> awaitingTasks;

    public LuaEvent()
    {
        eventListeners = new List<LuaEventConnection>();
        awaitingTasks = new List<GameSchedulerTask>();
    }

    public LuaEventConnection? ConnectWithCurrentSecurityIdentity(GameManager gameManager, DynValue dynValue, BaseScript? attacher)
    {
        SecurityIdentity? securityIdentity = gameManager.GameScheduler.GetCurrentSecurityIdentity();
        if (securityIdentity == null)
            return null;
        LuaEventConnection eventConnection = new LuaEventConnection(this, securityIdentity, gameManager, dynValue, attacher);
        eventListeners.Add(eventConnection);
        return eventConnection;
    }

    public void Fire(DynValue[] args)
    {
        OnBeforeFired?.Invoke(this, args);

        for (int i = 0; i < eventListeners.Count; i++)
        {
            LuaEventConnection eventConnection = eventListeners[i];
            GameManager gameManager = eventConnection.GameManager;
            BaseScript? attacher = eventConnection.Attacher;
            DynValue function = eventConnection.Handler;

            GameSchedulerTask task = gameManager.GameScheduler.Schedule(new ScriptSchedulerTask(attacher, function)
            {
                DebugName = $"Event Handler - {attacher?.ToString() ?? "<>"} - {function}",
                GameScheduler = gameManager.GameScheduler,
                Delegate = _ => SchedulerTaskResult.NotCompleted
            });

            task.Identity = eventConnection.Identity;
        }

        if (awaitingTasks.Count > 0)
        {
            for (int i = 0; i < awaitingTasks.Count; i++)
                awaitingTasks[i].IsWaiting = false;
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