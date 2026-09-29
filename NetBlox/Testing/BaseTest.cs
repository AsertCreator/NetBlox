using Script = NetBlox.Instances.Scripts.Script;

namespace NetBlox.Testing;

public abstract class BaseTest
{
    public readonly GameTestManager GameTestManager;

    public bool HasResult { get; private set; }

    public BaseTest(GameTestManager environment)
    {
        GameTestManager = environment;
    }

    public virtual void ReportSuccess()
    {
        if (!HasResult)
        {
            HasResult = true;
            GameTestManager.RecordResult(this, true, "", null);
        }
    }
    public virtual void ReportFailure_WrongAnswer() => ReportFailure(null, "Wrong answer");
    public virtual void ReportFailure_FatalException(Exception ex) => ReportFailure(ex, "Fatal exception");
    public virtual void ReportFailure_ScriptingFailure() => ReportFailure(null, "Scripting failure");
    public virtual void ReportFailure(Exception? ex, string subtype)
    {
        if (!HasResult)
        {
            HasResult = true;
            GameTestManager.RecordResult(this, false, subtype, ex);
        }
    }
    public virtual GameSchedulerTask BeginTest()
    {
        return GameTestManager.GameManager.GameScheduler.Schedule("Test - " + ToString(), GameScheduler.SchedulerPhase.Any, _ =>
        {
            try
            {
                Conduct();
                ReportSuccess();
                return SchedulerTaskResult.CompletedSuccess;
            }
            catch (Exception ex)
            {
                ReportFailure_FatalException(ex);
                return SchedulerTaskResult.CompletedFailed;
            }
        });
    }

    public abstract void Conduct();
}