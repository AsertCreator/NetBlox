using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class IntValue : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public long Value { get; set; }

    public override string ClassName => nameof(IntValue);

    public IntValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(IntValue))
            return base.IsA(className);
        return true;
    }
}