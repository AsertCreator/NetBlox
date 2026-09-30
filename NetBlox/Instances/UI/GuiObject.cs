using System.Numerics;
using MoonSharp.Interpreter;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

public class GuiObject : GuiBase2d
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool Active { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector2 AnchorPoint { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool Visible { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual UDim2 Position { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual UDim2 Size { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual int ZIndex { get; set; } = 1;

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual BrickColor BorderColor
    {
        get => BrickColor.GetBrickColorByClosestColor3(BorderColor3);
        set => BorderColor3 = value.Color3;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Color3 BorderColor3 { get; set; } = new Color3(10, 10, 20);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual int BorderSizePixel { get; set; } = 1;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual BrickColor BackgroundColor
    {
        get => BrickColor.GetBrickColorByClosestColor3(BackgroundColor3);
        set => BackgroundColor3 = value.Color3;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Color3 BackgroundColor3 { get; set; } = new Color3(255, 255, 255);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual float BackgroundTransparency { get; set; } = 0;

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual GuiObject? NextSelectionUp { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual GuiObject? NextSelectionDown { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual GuiObject? NextSelectionLeft { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual GuiObject? NextSelectionRight { get; set; }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool ClipsDescendants { get; set; }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseEnter { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseLeave { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseMoved { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseWheelBackward { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseWheelForward { get; init; } = new LuaEvent();

    public override Vector2 AbsolutePosition => absolutePosition;
    public override Vector2 AbsoluteSize => absoluteSize;

    private Vector2 absolutePosition;
    private Vector2 absoluteSize;
    protected int mouseOnMe = -1; // mouse on me

    public override string ClassName => nameof(GuiObject);

    public GuiObject(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public virtual void RenderUI(Rectangle containerRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        if (Visible)
        {
            Rectangle thisRectangle = ResolveBoundingRectangle(containerRectangle);
            
            if (ClipsDescendants)
            {
                cdclipStack.Push(currentClip);
                currentClip = currentClip.IntersectWith(thisRectangle);
                Raylib.BeginScissorMode((int)thisRectangle.Position.X, (int)thisRectangle.Position.Y, (int)thisRectangle.Size.X, (int)thisRectangle.Size.Y);
            }

            DoActualChildrenRendering(thisRectangle, cdclipStack, currentClip);

            if (ClipsDescendants)
            {
                Rectangle clip = cdclipStack.Pop();
                Raylib.BeginScissorMode((int)clip.Position.X, (int)clip.Position.Y, (int)clip.Size.X, (int)clip.Size.Y);
            }

            Vector2 mousePosition = Raylib.GetMousePosition();
            Rectangle boundingBox = new Rectangle(absolutePosition, absoluteSize);
            bool inside = boundingBox.ContainsPoint(mousePosition);
            if (!inside && mouseOnMe < 0)
                return;

            else if (inside && mouseOnMe < 0)
            {
                MouseEnter.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
                mouseOnMe = -mouseOnMe + 1;
            }
            else if (!inside && mouseOnMe >= 0)
            {
                MouseLeave.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
                mouseOnMe = -mouseOnMe;
            }
            else if (inside && mouseOnMe >= 0)
            {
                MouseMoved.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
            }
        }
    }

    public virtual void DoActualChildrenRendering(Rectangle thisRectangle, Stack<Rectangle> cdclipStack, Rectangle currentClip)
    {
        using RentedSpan<GuiObject> instances = GetChildrenGuiObjects_Fast();

        for (int i = 0; i < instances.Values.Length; i++)
            instances.Values[i].RenderUI(thisRectangle, cdclipStack, currentClip);
    }
    public virtual GuiObject? GetTopLevelGuiObject(Vector2 mouseposition)
    {
        using RentedSpan<GuiObject> instances = GetChildrenGuiObjects_Fast();
        Rectangle boundingBox = new Rectangle(absolutePosition, absoluteSize);
        bool inside = boundingBox.ContainsPoint(mouseposition);

        if (ClipsDescendants && !inside)
            return null;

        for (int i = 0; i < instances.Values.Length; i++)
        {
            GuiObject? result = instances.Values[i].GetTopLevelGuiObject(mouseposition);
            if (result != null)
                return result;
        }

        if (inside)
            return this;
        
        return null;
    }
    public virtual GuiObject? GetTopLevelScrollableGuiObject(Vector2 mouseposition)
    {
        using RentedSpan<GuiObject> instances = GetChildrenGuiObjects_Fast();
        Rectangle boundingBox = new Rectangle(absolutePosition, absoluteSize);
        bool inside = boundingBox.ContainsPoint(mouseposition);

        if (ClipsDescendants && !inside)
            return null;

        for (int i = 0; i < instances.Values.Length; i++)
        {
            GuiObject? result = instances.Values[i].GetTopLevelScrollableGuiObject(mouseposition);
            if (result != null)
                return result;
        }

        if (inside && this is ScrollingFrame)
            return this;
        
        return null;
    }
    public virtual RentedSpan<GuiObject> GetChildrenGuiObjects_Fast()
    {
        using RentedSpan<Instance?> rchildren = GetChildren_Fast();
        RentedSpan<GuiObject> rguiobjects = new RentedSpan<GuiObject>(rchildren.Values.Length);
        int i = 0, j = 0;

        for (; i < rchildren.Values.Length; i++) {
            GuiObject? gui = rchildren.Values[i] as GuiObject;
            if (gui != null)
                rguiobjects.Values[j++] = gui;
        }

        rguiobjects.Values = rguiobjects.Values.Slice(0, j);
        rguiobjects.Values.Sort((x, y) => x.ZIndex.CompareTo(y.ZIndex));
        
        return rguiobjects;
    }

    public virtual void InvokeMouseButton1Down() { /* nope */ }
    public virtual void InvokeMouseButton1Up() { /* nope */ }
    public virtual void InvokeMouseButton1Click() { /* nope */ }
    public virtual void InvokeMouseButton2Down() { /* nope */ }
    public virtual void InvokeMouseButton2Up() { /* nope */ }
    public virtual void InvokeMouseButton2Click() { /* nope */ }
    public virtual void InvokeMouseWheelBackward(float x, float y)
    {
        if (mouseOnMe > 0)
            MouseWheelBackward.Fire([DynValue.NewNumber(x), DynValue.NewNumber(y)]);
    }
    public virtual void InvokeMouseWheelForward(float x, float y)
    {
        if (mouseOnMe > 0)
            MouseWheelForward.Fire([DynValue.NewNumber(x), DynValue.NewNumber(y)]);
    }

    public Rectangle ResolveBoundingRectangle(Rectangle container)
    {
        Vector2 position = container.Position + Position.Resolve(container.Size);
        Vector2 size = Size.Resolve(container.Size);
        position -= size * AnchorPoint;

        absolutePosition = position;
        absoluteSize = size;

        return new Rectangle(position, size);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(GuiObject))
            return base.IsA(className);
        return true;
    }
}