using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class ObjectValue : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public ulong Value { get; set; }

    public override string ClassName => nameof(ObjectValue);

    public ObjectValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ObjectValue))
            return base.IsA(className);
        return true;
    }
}