namespace NetBlox.Instances.UI;

public class GuiBase : Instance
{
    public override string ClassName => nameof(GuiBase);

    public GuiBase(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(GuiBase))
            return base.IsA(className);
        return true;
    }
}