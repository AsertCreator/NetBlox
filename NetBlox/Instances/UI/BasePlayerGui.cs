using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

public class BasePlayerGui : Instance
{
    public override string ClassName => nameof(BasePlayerGui);

    public BasePlayerGui(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public void Render()
    {
        Instance[] instances = GetChildren();
        Stack<Rectangle> cdclipStack = [];
        bool finishedInteraction = false;

        for (int i = 0; i < instances.Length; i++)
        {
            ScreenGui? screenGui = instances[i] as ScreenGui;

            if (screenGui == null)
                continue;
            if (!screenGui.Enabled)
                continue;

            if (!finishedInteraction)
                finishedInteraction = screenGui.TestMouse_2();

            using RentedSpan<Instance?> allInstances = screenGui.GetChildren_Fast();
            Rectangle rectangle = new Rectangle();
            rectangle.Size = new System.Numerics.Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            for (int j = 0; j < allInstances.Values.Length; j++)
            {
                GuiObject? guiObject = allInstances.Values[j] as GuiObject;
                if (guiObject is null)
                    continue;

                guiObject.RenderUI(rectangle, cdclipStack, rectangle);
            }
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BasePlayerGui))
            return base.IsA(className);
        return true;
    }
}