using System.Numerics;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

[Creatable]
public class ScrollingFrame : GuiObject
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public UDim2 ContentSize { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Vector2 ContentPosition 
    { 
        get => -ScrollOffset;
        set => ScrollOffset = -value;
    }

    public override string ClassName => nameof(ScrollingFrame);

    private Vector2 ScrollOffset;

    public ScrollingFrame(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        ClipsDescendants = true;
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
    public override void DoActualChildrenRendering(Rectangle thisRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        thisRectangle.Position.X -= ScrollOffset.X;
        thisRectangle.Position.Y -= ScrollOffset.Y;
        base.DoActualChildrenRendering(thisRectangle, cdclipStack, currentClip);
    }
    public override void InvokeMouseWheelBackward(float x, float y)
    {
        Vector2 contentrectangle = ContentSize.Resolve(AbsoluteSize);
        Vector2 actualMousewheelMovE = Raylib.GetMouseWheelMoveV();

        ScrollOffset.X -= actualMousewheelMovE.X * Raylib.GetWindowScaleDPI().X * 3;   
        if (ScrollOffset.X < 0)
            ScrollOffset.X = 0;
        if (ScrollOffset.X > (contentrectangle - AbsoluteSize).X)
            ScrollOffset.X = (contentrectangle - AbsoluteSize).X;

        ScrollOffset.Y -= actualMousewheelMovE.Y * Raylib.GetWindowScaleDPI().Y * 3;   
        if (ScrollOffset.Y < 0)
            ScrollOffset.Y = 0;
        if (ScrollOffset.Y > (contentrectangle - AbsoluteSize).Y)
            ScrollOffset.Y = (contentrectangle - AbsoluteSize).Y;

        base.InvokeMouseWheelBackward(x, y);
    }
    public override void InvokeMouseWheelForward(float x, float y)
    {
        Vector2 contentrectangle = ContentSize.Resolve(AbsoluteSize);
        Vector2 actualMousewheelMovE = Raylib.GetMouseWheelMoveV();

        ScrollOffset.X -= actualMousewheelMovE.X * Raylib.GetWindowScaleDPI().X * 3;   
        if (ScrollOffset.X < 0)
            ScrollOffset.X = 0;
        if (ScrollOffset.X > (contentrectangle - AbsoluteSize).X)
            ScrollOffset.X = (contentrectangle - AbsoluteSize).X;

        ScrollOffset.Y -= actualMousewheelMovE.Y * Raylib.GetWindowScaleDPI().Y * 3;   
        if (ScrollOffset.Y < 0)
            ScrollOffset.Y = 0;
        if (ScrollOffset.Y > (contentrectangle - AbsoluteSize).Y)
            ScrollOffset.Y = (contentrectangle - AbsoluteSize).Y;

        base.InvokeMouseWheelForward(x, y);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ScrollingFrame))
            return base.IsA(className);
        return true;
    }
}