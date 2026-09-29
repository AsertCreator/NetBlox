using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class BoolValue : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool Value { get; set; }

    public override string ClassName => nameof(BoolValue);

    public BoolValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BoolValue))
            return base.IsA(className);
        return true;
    }
}