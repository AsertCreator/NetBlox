using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class Smoke : Instance
{
    public override string ClassName => nameof(Smoke);

    public Smoke(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Smoke))
            return base.IsA(className);
        return true;
    }
}