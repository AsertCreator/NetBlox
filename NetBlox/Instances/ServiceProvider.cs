using System.Diagnostics;
using NetBlox.Runtime;

namespace NetBlox.Instances;

public class ServiceProvider : Instance
{
    public override string ClassName => nameof(ServiceProvider);

    public ServiceProvider(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance GetService(string name)
    {
        Instance? service = FindService(name);
        if (service != null)
            return service;
        
        service = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(name);
        if (service == null)
            throw new InvalidProgramException("GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass somehow failed");
        service.Parent = this;
        service.ParentLocked = true;

        if (InitializationStage >= InitializationStage.Initializing)
            service.CommitStageInitialize();
        if (InitializationStage >= InitializationStage.Alive)
            service.CommitStageAlive();

        Trace.TraceInformation("Created " + name + " service");
        return service;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? FindService(string name)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance != null && instance.ClassName == name)
                return instance;
        }
        return null;
    }
    public T GetService<T>() where T : Instance
    {
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