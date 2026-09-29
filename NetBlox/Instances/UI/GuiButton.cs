using System.Numerics;
using MoonSharp.Interpreter;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances.UI;

public class GuiButton : GuiObject
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton1Down { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton1Up { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton1Click { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton2Down { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton2Up { get; init; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent MouseButton2Click { get; init; } = new LuaEvent();

    public override string ClassName => nameof(GuiButton);

    protected string? verbId;
    protected bool isPressed;

    private int origMouseOnMe1 = int.MinValue;
    private int origMouseOnMe2 = int.MinValue;

    public GuiButton(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public void SetVerb(string? verb) => verbId = verb;

    public override void InvokeMouseButton1Down()
    { 
        Vector2 mousePosition = Raylib.GetMousePosition();
        MouseButton1Down.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
        origMouseOnMe1 = mouseOnMe;
        isPressed = true;
    }
    public override void InvokeMouseButton1Up()
    { 
        Vector2 mousePosition = Raylib.GetMousePosition();
        MouseButton1Up.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
        if (origMouseOnMe1 == mouseOnMe)
            InvokeMouseButton1Click();
        isPressed = false;
    }
    public override void InvokeMouseButton1Click()
    { 
        if (verbId != null)
        {
            GameManager.GameScheduler.Schedule("InvokeMouseButton1Click - Verb", GameScheduler.SchedulerPhase.Any, _ =>
            {
                if (GameManager.TryInvokeVerb(verbId))
                    return SchedulerTaskResult.CompletedSuccess;
                return SchedulerTaskResult.CompletedFailed;
            });
        }
        MouseButton1Click.Fire([]);
    }
    public override void InvokeMouseButton2Down()
    { 
        Vector2 mousePosition = Raylib.GetMousePosition();
        MouseButton2Down.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
        origMouseOnMe2 = mouseOnMe;
    }
    public override void InvokeMouseButton2Up()
    { 
        Vector2 mousePosition = Raylib.GetMousePosition();
        MouseButton2Up.Fire([DynValue.NewNumber(mousePosition.X), DynValue.NewNumber(mousePosition.Y)]);
        if (origMouseOnMe2 == mouseOnMe)
            InvokeMouseButton1Click();
    }
    public override void InvokeMouseButton2Click()
    { 
        MouseButton2Click.Fire([]);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(GuiButton))
            return base.IsA(className);
        return true;
    }
}