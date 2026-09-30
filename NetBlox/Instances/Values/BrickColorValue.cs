using System.Numerics;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances.Values;

[Creatable]
public class BrickColorValue : ValueBase
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BrickColor Value { get; set; }

    public override string ClassName => nameof(BrickColorValue);

    public BrickColorValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BrickColorValue))
            return base.IsA(className);
        return true;
    }
}