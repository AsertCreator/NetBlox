using NetBlox.Instances.Parts;
using NetBlox.Runtime;

namespace NetBlox.Instances;

public class InstanceAdornment : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BasePart? Adornee { get; set; }

    public override string ClassName => nameof(InstanceAdornment);

    public InstanceAdornment(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(InstanceAdornment))
            return base.IsA(className);
        return true;
    }
}