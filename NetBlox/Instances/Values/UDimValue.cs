using System.Numerics;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances.Values;

[Creatable]
public class UDimValue : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public UDim Value { get; set; }

    public override string ClassName => nameof(UDimValue);

    public UDimValue(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(UDimValue))
            return base.IsA(className);
        return true;
    }
}