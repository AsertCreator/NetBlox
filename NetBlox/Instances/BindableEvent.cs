using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(67)]
public class BindableEvent : Instance
{
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent Event { get; set; } = new LuaEvent();

    public override string ClassName => nameof(BindableEvent);

    public BindableEvent(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void Fire(params DynValue[] arguments)
    {
        Event.Fire(arguments);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BindableFunction))
            return base.IsA(className);
        return true;
    }
}