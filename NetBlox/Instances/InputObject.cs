using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

public class InputObject : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public KeyCode KeyCode { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public UserInputState UserInputState { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public UserInputType UserInputType { get; set; }

    public override string ClassName => nameof(InputObject);

    public InputObject(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(InputObject))
            return base.IsA(className);
        return true;
    }
}