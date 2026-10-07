using System.Numerics;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances.UI;

public class LayerCollector : GuiBase2d
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool Enabled { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool ResetOnSpawn { get; set; }

    public override string ClassName => nameof(LayerCollector);

    public LayerCollector(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public GuiObject? GetCandidateAt(Vector2 vector2)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance == null)
                continue;
            GuiObject? guiObject = instance as GuiObject;
            if (guiObject == null)
                continue;
            GuiObject? candidate = guiObject.GetTopLevelGuiObject(vector2);
            if (candidate == null)
                continue;
            return candidate;
        }
        return null;
    }
    public GuiObject? GetScrollableCandidateAt(Vector2 vector2)
    {
        for (int i = 0; i < children.Count; i++)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(children[i]);
            if (instance == null)
                continue;
            GuiObject? guiObject = instance as GuiObject;
            if (guiObject == null)
                continue;
            GuiObject? candidate = guiObject.GetTopLevelScrollableGuiObject(vector2);
            if (candidate == null)
                continue;
            return candidate;
        }
        return null;
    }
    public bool TestMouse_2()
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        bool flag = false;
        
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            flag |= HandleMouseButton1Down(mousePosition);
        if (Raylib.IsMouseButtonUp(MouseButton.Left))
            flag |= HandleMouseButton1Up(mousePosition);
        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            flag |= HandleMouseButton2Down(mousePosition);
        if (Raylib.IsMouseButtonUp(MouseButton.Right))
            flag |= HandleMouseButton2Up(mousePosition);
        
        Vector2 vector2 = Raylib.GetMouseWheelMoveV();
        if (vector2.Y > 0)
            flag |= HandleMouseWheelForward(mousePosition, new Vector2(vector2.X, vector2.Y));
        else if (vector2.Y < 0)
            flag |= HandleMouseWheelBackward(mousePosition, new Vector2(vector2.X, vector2.Y));
        
        return flag;
    }
    public bool HandleMouseButton1Down(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        candidate?.InvokeMouseButton1Down();
        return candidate != null;
    }
    public bool HandleMouseButton1Up(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        candidate?.InvokeMouseButton1Up();
        return candidate != null;
    }
    public bool HandleMouseButton2Down(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        candidate?.InvokeMouseButton2Down();
        return candidate != null;
    }
    public bool HandleMouseButton2Up(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        candidate?.InvokeMouseButton2Up();
        return candidate != null;
    }
    public bool HandleMouseWheelBackward(Vector2 position, Vector2 vector2)
    {
        GuiObject? candidate = GetScrollableCandidateAt(position);
        // but roblox is stupid and passes the mouse position to the event for some reason
        candidate?.InvokeMouseWheelBackward(position.X, position.Y);
        return candidate != null;
    }
    public bool HandleMouseWheelForward(Vector2 position, Vector2 vector2)
    {
        GuiObject? candidate = GetScrollableCandidateAt(position);
        candidate?.InvokeMouseWheelForward(position.X, position.Y);
        return candidate != null;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(LayerCollector))
            return base.IsA(className);
        return true;
    }
}