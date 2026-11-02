using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

public class FaceInstance : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public NormalId Face { get; set; }

    public override string ClassName => nameof(FaceInstance);

    public FaceInstance(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(FaceInstance))
            return base.IsA(className);
        return true;
    }
}