using NetBlox.Instances;

namespace NetBlox;

public sealed class GameEvent
{
    public required GameManager GameManager;
    public required string DebugName;
    public required string Id;

    public static readonly string EVENT_PLAYERADDED = nameof(EVENT_PLAYERADDED);
    public static readonly string EVENT_PLAYERREMOVED = nameof(EVENT_PLAYERREMOVED);
    public static readonly string EVENT_LOCALPLAYERCHANGED = nameof(EVENT_LOCALPLAYERCHANGED);
    public static readonly string EVENT_HEARTBEAT = nameof(EVENT_HEARTBEAT);
    public static readonly string EVENT_BEFORE_RENDER = nameof(EVENT_BEFORE_RENDER);
    public static readonly string EVENT_RENDER3D = nameof(EVENT_RENDER3D);
    public static readonly string EVENT_RENDER3D_TRANSPARENT = nameof(EVENT_RENDER3D_TRANSPARENT);
    public static readonly string EVENT_RENDERGUI_LEVEL0 = nameof(EVENT_RENDERGUI_LEVEL0);
    public static readonly string EVENT_RENDERGUI_LEVEL1 = nameof(EVENT_RENDERGUI_LEVEL1);
    public static readonly string EVENT_RENDERGUI_LEVEL2 = nameof(EVENT_RENDERGUI_LEVEL2);
    public static readonly string EVENT_RENDERGUI_LEVEL3 = nameof(EVENT_RENDERGUI_LEVEL3);
    public static readonly string EVENT_AFTER_RENDER = nameof(EVENT_AFTER_RENDER);
    public static readonly string EVENT_BEFORE_PHYSICS = nameof(EVENT_BEFORE_PHYSICS);
    public static readonly string EVENT_AFTER_PHYSICS = nameof(EVENT_AFTER_PHYSICS);
    public static readonly string EVENT_LOG_INFO = nameof(EVENT_LOG_INFO);
    public static readonly string EVENT_LOG_WARN = nameof(EVENT_LOG_WARN);
    public static readonly string EVENT_KICKED = nameof(EVENT_KICKED);
    public static readonly string EVENT_NETWORKSERVER_STARTED = nameof(EVENT_NETWORKSERVER_STARTED);
    public static readonly string EVENT_NETWORKSERVER_STOPPED = nameof(EVENT_NETWORKSERVER_STOPPED);
    public static readonly string EVENT_NETWORKCLIENT_STARTED = nameof(EVENT_NETWORKCLIENT_STARTED);
    public static readonly string EVENT_NETWORKCLIENT_STOPPED = nameof(EVENT_NETWORKCLIENT_STOPPED);

    private List<ulong> eventListenerData = [];
    private List<ulong> instancesToRemove = [];

    public void Fire(object? eventData = null)
    {
        List<ulong> nonExistentKeys = [];

        for (int i = 0; i < instancesToRemove.Count; i++)
            eventListenerData.Remove(instancesToRemove[i]);
        
        instancesToRemove.Clear();

        for (int i = 0; i < eventListenerData.Count; i++)
        {
            ulong instanceId = eventListenerData[i];
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(instanceId);
            EngineEventArgs args = new EngineEventArgs()
            {
                GameEvent = this,
                EventData = eventData
            };

            if (instance == null)
            {
                nonExistentKeys.Add(instanceId);
                continue;
            }

            instance.OnRegisteredEvent(args);
        }

        for (int i = 0; i < nonExistentKeys.Count; i++)
            eventListenerData.Remove(nonExistentKeys[i]);
    }
    public void RegisterInstance(ulong instanceId)
    {
        if (eventListenerData.Contains(instanceId))
            throw new NotSupportedException("Trying to register the same instance again for one event");
        eventListenerData.Add(instanceId);
    }
    public void RegisterInstance(Instance instance)
    {
        RegisterInstance(instance.InstanceID);
    }
    public void UnregisterInstance(ulong instanceId)
    {
        instancesToRemove.Add(instanceId);
    }
    public void UnregisterInstance(Instance instance)
    {
        instancesToRemove.Add(instance.InstanceID);
    }
}