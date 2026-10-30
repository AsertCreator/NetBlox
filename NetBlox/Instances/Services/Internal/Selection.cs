using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

[Service]
public class Selection : Instance
{
    public override string ClassName => nameof(Selection);

    private Instance[] selectedInstances = [];

    public const ulong NETWORK_CONSTANT_ID = 105;

    public Selection(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.Plugin)]
    public Instance[] Get()
    {
        bool reevaluate = false;
        foreach (Instance instance in selectedInstances)
            if (instance.InitializationStage >= InitializationStage.Destroying)
                reevaluate = true;
        if (reevaluate)
        {
            List<Instance> newSelection = [];
            foreach (Instance instance in selectedInstances)
                if (instance.InitializationStage < InitializationStage.Destroying)
                    newSelection.Add(instance);
            selectedInstances = newSelection.ToArray();
        }
        return selectedInstances;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.Plugin)]
    public void Set(Instance[] instances)
    {
        selectedInstances = instances;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Selection))
            return base.IsA(className);
        return true;
    }
}