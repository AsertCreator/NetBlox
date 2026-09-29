using NetBlox.Runtime;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

[Creatable]
public class Frame : GuiObject
{
    public override string ClassName => nameof(Frame);

    public Frame(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override void RenderUI(Rectangle containerRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        if (Visible)
        {
            Rectangle rectangle = ResolveBoundingRectangle(containerRectangle);

            Color color = BackgroundColor3;
            color.A = (byte)(255 * (1 - BackgroundTransparency));
            Raylib.DrawRectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, color);

            if (BorderSizePixel > 0)
                Raylib.DrawRectangleLines((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, BorderColor3);

            base.RenderUI(containerRectangle, cdclipStack, currentClip);
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Frame))
            return base.IsA(className);
        return true;
    }
}