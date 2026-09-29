using NetBlox.Runtime;

namespace NetBlox.Instances.UI;

[Creatable]
public class ScreenGui : LayerCollector
{
    public override string ClassName => nameof(ScreenGui);

    public ScreenGui(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ScreenGui))
            return base.IsA(className);
        return true;
    }
}