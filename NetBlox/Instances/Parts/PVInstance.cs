namespace NetBlox.Instances.Parts;

public class PVInstance : Instance
{
    public override string ClassName => nameof(PVInstance);

    public PVInstance(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(PVInstance))
            return base.IsA(className);
        return true;
    }
}