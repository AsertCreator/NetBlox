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
    public void TestMouse()
    {
        Vector2 mousePosition = Raylib.GetMousePosition();
        
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            HandleMouseButton1Down(mousePosition);
        if (Raylib.IsMouseButtonUp(MouseButton.Left))
            HandleMouseButton1Up(mousePosition);
        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            HandleMouseButton2Down(mousePosition);
        if (Raylib.IsMouseButtonUp(MouseButton.Right))
            HandleMouseButton2Up(mousePosition);
        
        Vector2 vector2 = Raylib.GetMouseWheelMoveV();
        if (vector2.Y > 0)
            HandleMouseWheelForward(mousePosition, new Vector2(vector2.X, vector2.Y));
        else if (vector2.Y < 0)
            HandleMouseWheelBackward(mousePosition, new Vector2(vector2.X, vector2.Y));
    }
    public void HandleMouseButton1Down(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        if (candidate != null)
            candidate.InvokeMouseButton1Down();
    }
    public void HandleMouseButton1Up(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        if (candidate != null)
            candidate.InvokeMouseButton1Up();
    }
    public void HandleMouseButton2Down(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        if (candidate != null)
            candidate.InvokeMouseButton2Down();
    }
    public void HandleMouseButton2Up(Vector2 vector2)
    {
        GuiObject? candidate = GetCandidateAt(vector2);
        if (candidate != null)
            candidate.InvokeMouseButton2Up();
    }
    public void HandleMouseWheelBackward(Vector2 position, Vector2 vector2)
    {
        GuiObject? candidate = GetScrollableCandidateAt(position);
        // but roblox is stupid and passes the mouse position to the event for some reason
        if (candidate != null)
            candidate.InvokeMouseWheelBackward(position.X, position.Y);
    }
    public void HandleMouseWheelForward(Vector2 position, Vector2 vector2)
    {
        GuiObject? candidate = GetScrollableCandidateAt(position);
        if (candidate != null)
            candidate.InvokeMouseWheelForward(position.X, position.Y);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(LayerCollector))
            return base.IsA(className);
        return true;
    }
}