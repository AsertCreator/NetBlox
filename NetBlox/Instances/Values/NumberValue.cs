using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class NumberValue : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public double Value { get; set; }

    public override string ClassName => nameof(NumberValue);

    public NumberValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(NumberValue))
            return base.IsA(className);
        return true;
    }
}