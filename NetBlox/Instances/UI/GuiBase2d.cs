using System.Numerics;
using NetBlox.Runtime;

namespace NetBlox.Instances.UI;

public class GuiBase2d : GuiBase
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector2 AbsolutePosition { get; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual float AbsoluteRotation { get; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector2 AbsoluteSize { get; }

    public override string ClassName => nameof(GuiBase2d);

    public GuiBase2d(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(GuiBase2d))
            return base.IsA(className);
        return true;
    }
}