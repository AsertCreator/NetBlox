using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Testing;

public abstract class ScriptingTest : BaseTest
{
    public ScriptingTest(GameTestManager environment) : base(environment) { }

    public abstract string ScriptContent();

    public override void Conduct()
    {
    }
    public virtual SecurityIdentity ConductTestUnderIdentity()
    {
        return SecurityIdentity.SI_GameScript;
    }
    public override GameSchedulerTask BeginTest()
    {
        GameTestManager.GameManager.GameScheduler.BeginTracedSecurityOverride(ConductTestUnderIdentity(), "Starting a scripting test " + GetType());
        GameSchedulerTask task = 
            GameTestManager.GameManager.RootModel.GetService<ScriptContext>().StartTestingScriptWithCurrentIdentity(this, ScriptContent(), true);
        GameTestManager.GameManager.GameScheduler.EndTracedSecurityOverride();
        return task;
    }
}