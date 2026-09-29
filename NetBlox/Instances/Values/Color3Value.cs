using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances.Values;

[Creatable]
public class Color3Value : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 Value { get; set; }

    public override string ClassName => nameof(Color3Value);

    public Color3Value(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Color3Value))
            return base.IsA(className);
        return true;
    }
}