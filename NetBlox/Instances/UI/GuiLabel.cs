namespace NetBlox.Instances.UI;

public class GuiLabel : GuiObject
{
    public override string ClassName => nameof(GuiLabel);

    public GuiLabel(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(GuiLabel))
            return base.IsA(className);
        return true;
    }
}