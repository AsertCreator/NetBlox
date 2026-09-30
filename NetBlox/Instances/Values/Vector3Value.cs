using System.Numerics;
using NetBlox.Runtime;

namespace NetBlox.Instances.Values;

[Creatable]
public class Vector3Value : ValueBase
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Vector3 Value { get; set; }

    public override string ClassName => nameof(Vector3Value);

    public Vector3Value(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Vector3Value))
            return base.IsA(className);
        return true;
    }
}