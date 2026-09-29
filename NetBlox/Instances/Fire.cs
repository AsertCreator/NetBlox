using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

[Creatable]
public class Fire : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 Color { get; set; } = new Color3(236, 139, 70);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool Enabled { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float Heat { get; set; } = 9;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Color3 SecondaryColor { get; set; } = new Color3(139, 80, 55);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float Size { get; set; } = 5;

    public override string ClassName => nameof(Fire);

    public Fire(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Fire))
            return base.IsA(className);
        return true;
    }
}