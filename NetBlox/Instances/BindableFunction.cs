using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(66)]
public class BindableFunction : Instance
{
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public DynValue OnInvoke 
    { 
        get
        {
            if (onInvoke.IsNil())
                return DynValue.Nil;
            return DynValue.NewCallback((_, _) =>
            {
                throw new InvalidOperationException("Do not call BindableFunction.OnInvoke directly");
            });
        }
        set
        {
            if (value.IsNil() || value.Type == DataType.Function)
                onInvoke = value;
            throw new InvalidOperationException("BindableFunction.OnInvoke can only be a function or nil");
        }
    }

    private DynValue onInvoke = DynValue.Nil;

    public override string ClassName => nameof(BindableFunction);

    public BindableFunction(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public DynValue Invoke(params DynValue[] arguments)
    {
        if (onInvoke.Type == DataType.Function)
            return onInvoke.Function.Call(arguments);
        throw new InvalidOperationException("Cannot call BindableFunction with no callback");
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BindableFunction))
            return base.IsA(className);
        return true;
    }
}