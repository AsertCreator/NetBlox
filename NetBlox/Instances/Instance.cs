using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Instances;

/// <summary>
/// Root class for all Instance's in NetBlox. Here's some rules for writing them properly:<br/>
/// <list type="bullet">
/// <item>You must override ClassName and IsA to reflect your class</item>
/// <item>Do NOT contain raw Instance references in fields/autoproperties. Store the reference as ulong of the instance's ID. Having properties
///       that automatically convert these ID's into Instance objects is OK, though.</item>
/// </list>
/// If you don't follow them then you will be hereby cursed with endless sleepless bugfixing nights
/// </summary>
public class Instance
{
    public struct InstanceInitializationSettings
    {
        public bool IsForeign;
        public bool HasEverBeenAPartOfReplicatableDataModel;
    }

    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Instance? Parent
    {
        get => GameManager.GameRegistry.GetLocalInstanceById(parentid);
        set
        {
            if (ParentLocked && !GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
                throw new Exception("Parent property of this Instance is locked");

            Instance? myParent = GameManager.GameRegistry.GetLocalInstanceById(parentid);

            if (value != null && (value.IsDescendantOf(this) || value == this))
                throw new InvalidOperationException("New parent of an Instance cannot be said Instance's descendant or itself");

            if (value != null && !value.AskToBeParent(this))
                return;
            
            if (value != null && value.Root != Root)
                throw new InvalidOperationException("New parent of an Instance cannot be said an Instance belonging to another game");

            if (value != myParent)
            {
                myParent?.children.Remove(InstanceID);
                myParent?.OnChildRemoved(this);
                value?.children.Add(InstanceID);
                value?.OnChildAdded(this);

                if (value is not null)
                {
                    ViewportIndex = value.ViewportIndex;
                    parentid = value.InstanceID;
                }
                else
                {
                    ViewportIndex = -1;
                    parentid = 0;
                }

                if (InitializationSettings.HasEverBeenAPartOfReplicatableDataModel && Replicatable)
                {
                    ReplicationAgent? agent = ReplicationAgentIfServer;
                    if (agent != null)
                        agent.DeltaReparentInstance(this);
                }
                if (IsPartOfDataModel && !InitializationSettings.HasEverBeenAPartOfReplicatableDataModel && Replicatable)
                {
                    InitializationSettings.HasEverBeenAPartOfReplicatableDataModel = true;
                    ReplicationAgentIfServer?.DeltaAddNewInstance(this);
                }

                InvokeAncestryChanged(this, value);
            }
        }
    }

    public bool IsPartOfDataModel
    {
        get
        {
            if (Parent == null && ClassName == "DataModel")
                return true;
            if (Parent == null)
                return false;
            return Parent.IsPartOfDataModel;
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool Archivable { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Name { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual string ClassName => nameof(Instance);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent ChildAdded { get; set; } = new();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent ChildRemoved { get; set; } = new();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent DescendantAdded { get; set; } = new();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent DescendantRemoved { get; set; } = new();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent AncestryChanged { get; set; } = new();

    public bool ParentLocked;
    public int ViewportIndex;
    public InstanceInitializationSettings InitializationSettings;
    public InitializationStage InitializationStage = InitializationStage.Newborn;
    public readonly InstanceID InstanceID;
    public readonly GameManager GameManager;

    public DataModel Root => GameManager.RootModel;
    public Instance? ParentService
    {
        get
        {
            if (Parent == null)
                return null;
            if (Parent == Root)
                return this;
            return Parent.ParentService;
        }
    }

    public ReplicationAgent? ReplicationAgent => ReplicationAgentIfClient ?? ReplicationAgentIfServer;
    public ReplicationAgent? ReplicationAgentIfClient
    {
        get
        {
            if (GameManager.NetworkMode == NetworkMode.Client)
            {
                NetworkClient? networkClient = Root.FindService<NetworkClient>();
                if (networkClient == null)
                    return null;
                return networkClient.ReplicationAgent;
            }
            return null;
        }
    }
    public ReplicationAgent? ReplicationAgentIfServer
    {
        get
        {
            if (GameManager.NetworkMode == NetworkMode.Server)
            {
                NetworkServer? networkServer = Root.FindService<NetworkServer>();
                if (networkServer == null)
                    return null;
                return networkServer.ReplicationAgent;
            }
            return null;
        }
    }
    public bool Replicatable
    {
        get
        {
            ScriptContext scriptContext = Root.GetService<ScriptContext>();
            InstanceBridge.InstanceClassCache cache = scriptContext.ResolveInstanceClassCacheForType(GetType());

            if (!cache.CanBeReplicated)
                return false;
            if (Parent == Root && cache.CanBeReplicated && cache.CanChildrenBeReplicated)
                return true;
            
            return Parent != null ? Parent.Replicatable : false;
        }
    }

    public Instance[] EvalutedChildren => GetChildren();

    public GlobalTimestamp ShouldBeDestroyedBy = GlobalTimestamp.MaxValue;

    public ulong parentid = 0;
    public readonly List<ulong> children = [];
    public readonly List<GameEvent> registeredEvents = [];

    public Instance(ulong id, GameManager gameManager)
    {
        InstanceID = id;
        GameManager = gameManager;
        Name = ClassName;

        CommitStageNewborn();
    }

    protected void RegisterForEventId(string eventId)
    {
        GameEvent? gameEvent = GameManager.TryGetEventForId(eventId);
        if (gameEvent == null)
            throw new NotSupportedException("No such event found " + eventId + "!");
        gameEvent.RegisterInstance(InstanceID);
        registeredEvents.Add(gameEvent);
    }

    public RentedSpan<Instance?> GetChildren_Fast()
    {
        RentedSpan<Instance?> span = new RentedSpan<Instance?>(children.Count);
        for (int i = 0; i < children.Count; i++)
            span.Values[i] = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
        return span;
    }

    //
    //  Below are Lua API functions
    //

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? Clone()
    {
        Dictionary<ulong, ulong> oldAndNewMap = [];
        List<(Instance Target, InstanceBridge.InstanceClassCacheProperty Property, ulong Value)> pendingFixups = [];

        Instance? DoClone(Instance instance)
        {
            if (!Archivable)
                return null;
            if (Parent == Root)
                return null;
            if (this == Root)
                return null;
                
            Instance? newInstance = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(ClassName);
            if (newInstance == null)
                return null;
            
            oldAndNewMap[instance.InstanceID] = newInstance.InstanceID;
            
            InstanceBridge.InstanceClassCache icc = Root.GetService<ScriptContext>().ResolveInstanceClassCacheForType(GetType());
            foreach (InstanceBridge.InstanceClassCacheProperty property in icc.Properties.Values)
            {
                if (property.Name == "Parent")
                    continue;
                if (property.IsReadOnly)
                    continue;
                object? value = property.Property.GetValue(this);
                if (property.PropertyType == typeof(ulong))
                {
                    ulong castValue = (ulong)value!;
                    if (castValue == 0)
                        continue;
                    pendingFixups.Add((newInstance, property, (ulong)value!));
                }
                else if (property.PropertyType.IsAssignableTo(typeof(Instance)))
                {
                    Instance castValue = (Instance)value!;
                    if (castValue == null)
                        continue;
                    pendingFixups.Add((newInstance, property, castValue.InstanceID));
                }
                else
                    property.Property.SetValue(newInstance, value);
            }

            Instance[] children = instance.GetChildren();

            for (int i = 0; i < children.Length; i++)
            {
                Instance child = children[i];
                Instance? childClone = DoClone(child);
                if (childClone == null)
                    continue;
                childClone.Parent = newInstance;
            }

            return newInstance;
        }

        Instance? rootClone = DoClone(this);
        if (rootClone == null)
            return null;
        
        for (int i = 0; i < pendingFixups.Count; i++)
        {
            var pendingFixup = pendingFixups[i];
            Instance? resolvedReferent = null;

            if (oldAndNewMap.TryGetValue(pendingFixup.Value, out ulong clonedInstance))
                GameManager.GameRegistry.GetLocalInstanceById(clonedInstance);
            else
                GameManager.GameRegistry.GetLocalInstanceById(pendingFixup.Value);

            if (pendingFixup.Property.PropertyType == typeof(ulong))
                pendingFixup.Property.Property.SetValue(pendingFixup.Target, resolvedReferent!.InstanceID);
            else
                pendingFixup.Property.Property.SetValue(pendingFixup.Target, resolvedReferent);
        }

        return rootClone;
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstChild(string name)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance == null)
                continue;
            if (instance.Name == name)
                return instance;
        }
        return null;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstChildOfClass(string className)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance == null)
                continue;
            if (instance.ClassName == className)
                return instance;
        }
        return null;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstChildWhichIsA(string className)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance == null)
                continue;
            if (instance.IsA(className))
                return instance;
        }
        return null;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstAncestor(string name)
    {
        if (Parent == null)
            return null;
        if (Parent.Name == name)
            return Parent;
        return Parent.FindFirstAncestor(name);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstAncestorOfClass(string className)
    {
        if (Parent == null)
            return null;
        if (Parent.ClassName == className)
            return Parent;
        return Parent.FindFirstAncestorOfClass(className);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindFirstAncestorWhichIsA(string className)
    {
        if (Parent == null)
            return null;
        if (Parent.IsA(className))
            return Parent;
        return Parent.FindFirstAncestorWhichIsA(className);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsDescendantOf(Instance instance)
    {
        if (Parent == null)
            return false;
        if (Parent == instance)
            return true;
        return Parent.IsDescendantOf(instance);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsAncestorOf(Instance instance)
    {
        return instance.IsDescendantOf(this);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Instance[] GetChildren()
    {
        List<Instance> instances = [];
        List<ulong> deadChildren = [];
        for (int i = 0; i < children.Count; i++)
        {
            ulong childId = children[i];
            Instance? childRef = GameManager.GameRegistry.GetLocalInstanceById(childId);
            if (childRef == null)
                deadChildren.Add(childId);
            else
                instances.Add(childRef);
        }
        for (int i = 0; i < deadChildren.Count; i++)
            children.Remove(deadChildren[i]);
        return instances.ToArray();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string GetFullName()
    {
        if (Parent == null || Parent.ClassName == "DataModel")
            return Name;
        return Parent.GetFullName() + "." + Name;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool IsA(string className)
    {
        if (className == "Instance" || className == "Object" || className == "<<<ROOT>>>")
            return true;
        return false;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Instance[] GetDescendants()
    {
        List<Instance> instances = new List<Instance>();

        void Process(Instance instance)
        {
            Instance[] childrenPool = instance.GetChildren();

            instances.AddRange(childrenPool);

            for (int i = 0; i < childrenPool.Length; i++)
                Process(childrenPool[i]);
        }

        return instances.ToArray();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual void Destroy()
    {
        InitializationStage = InitializationStage.Destroying;

        if (InitializationSettings.HasEverBeenAPartOfReplicatableDataModel)
        {
            ReplicationAgent? agent = ReplicationAgentIfServer;
            if (agent != null)
                agent.DeltaRemoveInstance(this);
        }

        CommitStageDestroying();

        for (int i = 0; i < registeredEvents.Count; i++)
        {
            GameEvent gameEvent = registeredEvents[i];
            gameEvent.UnregisterInstance(this);
        }

        Parent = null;
        ParentLocked = true;

        ulong[] immutable = new ulong[children.Count];
        children.CopyTo(immutable);
        for (int i = 0; i < immutable.Length; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(immutable[i]);
            if (instance == null)
                continue;
            instance.Destroy();
        }

        GameManager.GameRegistry.Remove(InstanceID);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual void ClearAllChildren()
    {
        ulong[] immutable = new ulong[children.Count];
        children.CopyTo(immutable);
        for (int i = 0; i < immutable.Length; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(immutable[i]);
            if (instance == null)
                continue;
            instance.ClearAllChildren();
            instance.Parent = null;
        }
    }
    /// <summary>
    /// NetBlox API extension, not in Roblox.
    /// </summary>
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual void DestroyAllChildren()
    {
        ulong[] immutable = new ulong[children.Count];
        children.CopyTo(immutable);
        for (int i = 0; i < immutable.Length; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(immutable[i]);
            if (instance == null)
                continue;
            instance.DestroyAllChildren();
            instance.Destroy();
        }
    }

    //
    //  Below are methods and properties marked by Roblox as deprecated
    //

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public string className => ClassName;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public LuaEvent childAdded => ChildAdded;

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public void Remove()
    {
        Parent = null;
        ClearAllChildren();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public Instance? findFirstChild(string name) => FindFirstChild(name);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public void destroy() => Destroy();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public void remove() => Remove();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public Instance[] getChildren() => GetChildren();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public bool isDescendantOf(Instance instance) => IsDescendantOf(instance);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    [Obsolete]
    public bool isA(string className) => IsA(className);

    //
    //  Below are Event methods
    //

    public void InvokeChildAdded(Instance value)
    {
        ChildAdded.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, value))]);
    }
    public void InvokeChildRemoved(Instance value)
    {
        ChildRemoved.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, value))]);
    }
    public void InvokeDescendantAdded(Instance value)
    {
        DescendantAdded.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, value))]);
        if (Parent == null)
            return;
        Parent.InvokeDescendantAdded(value);
    }
    public void InvokeDescendantRemoved(Instance value)
    {
        DescendantRemoved.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, value))]);
        if (Parent == null)
            return;
        Parent.InvokeDescendantRemoved(value);
    }
    public void InvokeAncestryChanged(Instance child, Instance? value)
    {
        AncestryChanged.Fire([
            DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, child)),
            value != null ? DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, value)) : DynValue.Nil
        ]);
        Instance[] children = GetChildren();
        for (int i = 0; i < children.Length; i++)
            children[i].InvokeAncestryChanged(child, value);
    }

    /// <summary>
    /// This method is called when the engine creates this object. Do not hook into engine functions and events just yet!
    /// </summary>
    protected virtual void CommitStageNewborn()
    {
        InitializationStage = InitializationStage.Newborn;
    }
    /// <summary>
    /// This method is called when the engine begins to initialize objects. Hook into engine functions and event here.
    /// </summary>
    public virtual void CommitStageInitialize()
    {   
        InitializationStage = InitializationStage.Initializing;
    }
    public virtual void CommitStageAlive()
    {   
        InitializationStage = InitializationStage.Alive;
    }
    public virtual void CommitStageDestroying()
    {
        InitializationStage = InitializationStage.Destroying;
    }

    public virtual bool AskToBeParent(Instance child)
    {
        return true;
    }

    public virtual void OnBeforePhysics()
    {
    }
    public virtual void OnAfterPhysics()
    {
    }
    public virtual void OnBeforeRendering()
    {
    }
    public virtual void OnAfterRendering()
    {
    }
    public virtual void OnChildRemoved(Instance who)
    {
        InvokeChildRemoved(who);
        InvokeDescendantRemoved(who);
    }
    public virtual void OnChildAdded(Instance who)
    {
        InvokeChildAdded(who);
        InvokeDescendantAdded(who);
    }
    public virtual void OnRegisteredEvent(EngineEventArgs args)
    {
        if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_RENDER)
            OnBeforeRendering();
        if (args.GameEvent.Id == GameEvent.EVENT_AFTER_RENDER)
            OnAfterRendering();
    }
}