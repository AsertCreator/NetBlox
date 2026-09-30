namespace NetBlox.Instances.Values;

public class ValueBase : Instance
{
    public override string ClassName => nameof(ValueBase);

    public ValueBase(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ValueBase))
            return base.IsA(className);
        return true;
    }
}