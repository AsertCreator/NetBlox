using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

[Creatable]
public class Team : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool AutoAssignable { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public int Score { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BrickColor TeamColor { get; set; } = BrickColor.Random();

    public override string ClassName => nameof(Team);

    public Team(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Team))
            return base.IsA(className);
        return true;
    }
}