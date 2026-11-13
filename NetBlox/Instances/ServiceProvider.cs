using System.Diagnostics;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Instances;

public class ServiceProvider : Instance
{
    public override string ClassName => nameof(ServiceProvider);

    public ServiceProvider(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public bool IsServiceAccessibleInThisIdentity(Instance instance)
    {
        ScriptContext scriptContext = GetService<ScriptContext>();
        InstanceBridge.InstanceClassCache classCache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());
        if (classCache.ServiceAccessibleToAttribute != null)
        {
            // we will gatekeep the service
            if (!GameManager.GameScheduler.GetCurrentSecurityIdentity().RequireSimpleCapability(classCache.ServiceAccessibleToAttribute.RequiredLevel))
                return false;
        }
        return true;
    }

    public override Instance[] GetChildren()
    {
        List<Instance> betterInstanceList = new List<Instance>();
        betterInstanceList.AddRange(base.GetChildren());
        
        for (int i = 0; i < betterInstanceList.Count; i++)
        {
            if (!IsServiceAccessibleInThisIdentity(betterInstanceList[i]))
                betterInstanceList.RemoveAt(i--);
        }

        return betterInstanceList.ToArray();
    }

    public override Instance? FindFirstChild(string name)
    {
        Instance? instance = base.FindFirstChild(name);
        if (instance == null)
            return null;
        if (!IsServiceAccessibleInThisIdentity(instance))
            return null;
        return instance;
    }
    public override Instance? FindFirstChildOfClass(string className)
    {
        Instance? instance = base.FindFirstChildOfClass(className);
        if (instance == null)
            return null;
        if (!IsServiceAccessibleInThisIdentity(instance))
            return null;
        return instance;
    }
    public override Instance? FindFirstChildWhichIsA(string className)
    {
        Instance? instance = base.FindFirstChildWhichIsA(className);
        if (instance == null)
            return null;
        if (!IsServiceAccessibleInThisIdentity(instance))
            return null;
        return instance;
    }

    [LuaAlias(Name = "service")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? GetService(string name)
    {
        Instance? service = FindService(name);
        if (service != null)
        {
            if (!IsServiceAccessibleInThisIdentity(service))
                return null;
            return service;
        }
        
        service = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(name);
        if (service == null)
            return null;
            
        service.Parent = this;
        service.ParentLocked = true;

        if (InitializationStage >= InitializationStage.Initializing)
            service.CommitStageInitialize();
        if (InitializationStage >= InitializationStage.Alive)
            service.CommitStageAlive();

        Trace.TraceInformation("Created " + name + " service");

        if (!IsServiceAccessibleInThisIdentity(service))
            return null;

        return service;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindService(string name)
    {
        Instance? possibleCandidate = FindFirstChild_Fast(name);
        if (possibleCandidate != null)
        {
            if (!IsServiceAccessibleInThisIdentity(possibleCandidate))
                return null;
            return possibleCandidate;
        }

        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);

            if (instance != null && instance.ClassName == name)
            {
                if (!IsServiceAccessibleInThisIdentity(instance))
                    return null;
                return instance;
            }
        }

        return null;
    }
    private string ResolveServiceNameForFastLookup<T>()
    {
        if (typeof(T) == typeof(ScriptContext))
            return "Script Context";
        if (typeof(T) == typeof(RunService))
            return "Run Service";
        return typeof(T).Name;
    }
    public T GetService<T>() where T : Instance
    {
        Instance? possibleCandidate = FindFirstChild_Fast(ResolveServiceNameForFastLookup<T>());
        if (possibleCandidate is T t)
            return t;

        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance is T foundservice)
                return foundservice;
        }
        
        T? service = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(typeof(T).Name) as T;
        if (service == null)
            throw new InvalidProgramException("GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass returns non-T for T");
            
        service.Parent = this;
        service.ParentLocked = true;

        if (InitializationStage >= InitializationStage.Initializing)
            service.CommitStageInitialize();
        if (InitializationStage >= InitializationStage.Alive)
            service.CommitStageAlive();

        Trace.TraceInformation("Created " + typeof(T) + " service");
        return service;
    }
    public T? FindService<T>() where T : Instance
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance is T foundservice)
                return foundservice;
        }
        return null;
    }
    
    public override bool AskToBeParent(Instance child)
    {
        if (child.InstanceID < GameRegistry.SERVICE_ID_BOUNDARY)
            return true;
        return false;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ServiceProvider))
            return base.IsA(className);
        return true;
    }
}