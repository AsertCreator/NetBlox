using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts;

public class BaseScript : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.Plugin)]
    public string Source { get; set; } = "";

    public override string ClassName => nameof(BaseScript);

    public Table? LocalEnvironment;

    protected bool hadAlreadyExecuted;

    public BaseScript(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public void ClearExecutedFlag()
    {
        hadAlreadyExecuted = false;
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
    }
    protected virtual void ExecutionOpportunity(SecurityIdentity? identity)
    {
        if (!hadAlreadyExecuted)
        {
            hadAlreadyExecuted = true;

            if (identity != null)
                GameManager.GameScheduler.BeginTracedSecurityOverride(identity, "Starting a script");
            if (LocalEnvironment != null)
                LocalEnvironment = Root.GetService<ScriptContext>().CreateNewLocalEnvironment(this);
            Root.GetService<ScriptContext>().StartScriptWithCurrentIdentity(this, true);
            if (identity != null)
                GameManager.GameScheduler.EndTracedSecurityOverride();
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BaseScript))
            return base.IsA(className);
        return true;
    }
}