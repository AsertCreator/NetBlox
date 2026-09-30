using System.Numerics;
using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class Vector2Value : ValueBase
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Vector2 Value { get; set; }

    public override string ClassName => nameof(Vector2Value);

    public Vector2Value(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Vector2Value))
            return base.IsA(className);
        return true;
    }
}