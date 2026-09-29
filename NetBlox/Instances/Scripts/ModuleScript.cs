using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts;

[Creatable]
public class ModuleScript : BaseScript
{
    public override string ClassName => nameof(ModuleScript);

    public ModuleScript(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    protected override void ExecutionOpportunity(SecurityIdentity? identity)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ModuleScript))
            return base.IsA(className);
        return true;
    }
}