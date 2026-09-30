using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class StringValue : ValueBase
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Value { get; set; } = "";

    public override string ClassName => nameof(StringValue);

    public StringValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(StringValue))
            return base.IsA(className);
        return true;
    }
}