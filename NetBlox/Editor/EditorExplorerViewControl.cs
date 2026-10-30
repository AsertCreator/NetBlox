using System.Numerics;
using System.Reflection;
using NetBlox.Instances;
using NetBlox.Instances.Services.Internal;
using NetBlox.Rendering;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Editor;

public class EditorExplorerViewControl : EditorControl
{
    public float ScrollOffset;
    public Dictionary<Instance, bool> InstanceState = [];
    public Dictionary<Type, ExplorerIconIndexAttribute> ExplorerIconIndexAttributeMap = [];

    public override void Render(CoreEditorService service, EditorWindow window, Vector2 offset, Vector2 windowSize)
    {
        FontSpecification fontSpecification = service.GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = service.GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 actualMousewheelMovE = Raylib.GetMouseWheelMoveV();
        Vector2 size = Size.Resolve(windowSize);

        Vector2 mousePosition = Raylib.GetMousePosition();

        Instance[] selection = service.Root.GetService<Selection>().Get();

        float y = 0;
        float x = 0;

        void RenderItem(Instance instance)
        {
            Vector2 completePosition = offset + new Vector2(x, y - ScrollOffset);

            if (completePosition.Y >= offset.Y - fontSpecification.Size && completePosition.Y < offset.Y + size.Y)
            {
                if (selection.Contains(instance))
                    Raylib.DrawRectangle((int)completePosition.X, (int)completePosition.Y, (int)size.X, (int)(fontSpecification.Size + 3), Color.Blue);
                
                Raylib.DrawTextEx(font, instance.Name + " - " + instance.ClassName, completePosition + new Vector2(fontSpecification.Size + 4, 0) * 2,
                    fontSpecification.Size, 0, Color.White);
                
                Type instanceType = instance.GetType();
                if (!ExplorerIconIndexAttributeMap.ContainsKey(instanceType))
                    ExplorerIconIndexAttributeMap[instanceType] = instanceType.GetCustomAttribute<ExplorerIconIndexAttribute>()!;
                
                int explorerIconIndex = ExplorerIconIndexAttributeMap[instanceType].Index;

                Raylib.DrawTexturePro(service.InstanceClassAtlas,
                    new Rectangle(16 * explorerIconIndex, 0, 16, 16), new Rectangle(completePosition + new Vector2(fontSpecification.Size + 4, 0), 16, 16),
                    default, 0, Color.White);
                
                if (instance.AliveChildrenCount > 0)
                {
                    if (!InstanceState.ContainsKey(instance))
                        Raylib.DrawRectangle(
                            (int)completePosition.X + 3, (int)completePosition.Y + 3, (int)fontSpecification.Size - 3, (int)fontSpecification.Size - 3,
                            Color.Red);
                    else
                        Raylib.DrawRectangle(
                            (int)completePosition.X + 3, (int)completePosition.Y + 3, (int)fontSpecification.Size - 3, (int)fontSpecification.Size - 3,
                            InstanceState[instance] ? Color.Green : Color.Red);
                }
            
                // handle Expand/Close button click

                if (mousePosition.X >= completePosition.X && mousePosition.X <= completePosition.X + fontSpecification.Size + 3 &&
                    mousePosition.Y >= completePosition.Y && mousePosition.Y <= completePosition.Y + fontSpecification.Size + 2 &&
                    Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (!InstanceState.ContainsKey(instance))
                        InstanceState[instance] = true;
                    else
                        InstanceState[instance] = !InstanceState[instance];
                }
                if (mousePosition.X > completePosition.X + fontSpecification.Size + 3 &&
                    mousePosition.Y >= completePosition.Y && mousePosition.Y <= completePosition.Y + fontSpecification.Size + 3 &&
                    Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    service.Root.GetService<Selection>().Set([instance]);
                }
            }

            y += fontSpecification.Size + 2;

            using RentedSpan<Instance?> rentedChildren = instance.GetChildren_Fast();
            
            if (InstanceState.ContainsKey(instance) && InstanceState[instance])
            {
                x += 20;

                for (int i = 0; i < rentedChildren.Values.Length; i++)
                {
                    Instance? childInstance = rentedChildren.Values[i];
                    if (childInstance == null)
                        continue;
                    RenderItem(childInstance);
                }

                x -= 20;
            }
        }

        using RentedSpan<Instance?> rentedDataModelChildren = service.Root.GetChildren_Fast();

        for (int i = 0; i < rentedDataModelChildren.Values.Length; i++)
        {
            Instance? childInstance = rentedDataModelChildren.Values[i];
            if (childInstance == null)
                continue;
            RenderItem(childInstance);
        }

        ScrollOffset -= actualMousewheelMovE.Y * Raylib.GetWindowScaleDPI().Y * 3;   
        if (ScrollOffset < 0)
            ScrollOffset = 0;
        if (ScrollOffset > y - size.Y)
            ScrollOffset = y - size.Y;
        if (y < size.Y)
            ScrollOffset = 0;
    }
}

/*
reference

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

*/