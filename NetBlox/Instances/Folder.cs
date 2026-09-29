using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class Folder : Instance
{
    public override string ClassName => nameof(Folder);

    public Folder(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Folder))
            return base.IsA(className);
        return true;
    }
}