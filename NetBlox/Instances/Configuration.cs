using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class Configuration : Instance
{
    public override string ClassName => nameof(Configuration);

    public Configuration(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Configuration))
            return base.IsA(className);
        return true;
    }
}