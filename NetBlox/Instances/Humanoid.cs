using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class Humanoid : Instance
{
    public override string ClassName => nameof(Humanoid);

    public Humanoid(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Humanoid))
            return base.IsA(className);
        return true;
    }
}