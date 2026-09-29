using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class ForceField : Instance
{
    public override string ClassName => nameof(ForceField);

    public ForceField(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ForceField))
            return base.IsA(className);
        return true;
    }
}