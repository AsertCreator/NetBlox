using NetBlox.Runtime;

namespace NetBlox.Instances.Parts;

[Creatable]
public class SpawnLocation : Part
{
    public override string ClassName => nameof(SpawnLocation);

    public SpawnLocation(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(SpawnLocation))
            return base.IsA(className);
        return true;
    }
}