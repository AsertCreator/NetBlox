using NetBlox.Instances.Parts;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

public class JointInstance : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CoordinateFrame C0 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CoordinateFrame C1 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BasePart? Part0
    {
        get => GameManager.GameRegistry.GetLocalInstanceById(part0) as BasePart;
        set
        {
            if (value == null)
                part0 = 0;
            else
                part0 = value.InstanceID;
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BasePart? Part1
    {
        get => GameManager.GameRegistry.GetLocalInstanceById(part1) as BasePart;
        set
        {
            if (value == null)
                part1 = 0;
            else
                part1 = value.InstanceID;
        }
    }

    protected InstanceID part0;
    protected InstanceID part1;

    public override string ClassName => nameof(JointInstance);

    public JointInstance(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(JointInstance))
            return base.IsA(className);
        return true;
    }
}