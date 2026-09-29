using System.Numerics;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances.Values;

[Creatable]
public class UDim2Value : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public UDim2 Value { get; set; }

    public override string ClassName => nameof(UDim2Value);

    public UDim2Value(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(UDim2Value))
            return base.IsA(className);
        return true;
    }
}