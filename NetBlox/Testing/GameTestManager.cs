using NetBlox.Runtime;

namespace NetBlox.Testing;

public struct TestResult
{
    public BaseTest baseTest;
    public bool success;
    public string subtype;
    public Exception? exception;
}
public sealed class GameTestManager
{
    public GameManager GameManager { get; init; }
    public List<BaseTest> AllPossibleTests = new List<BaseTest>();
    public Dictionary<BaseTest, TestResult> AllTestResults = new Dictionary<BaseTest, TestResult>();
    
    public GameTestManager(GameManager gameManager)
    {
        GameManager = gameManager;

        LoadTestSet(0);
    }

    public void RecordResult(BaseTest test, bool success, string subtype, Exception? exception)
    {
        AllTestResults[test] = new TestResult()
        {
            baseTest = test,
            success = success,
            subtype = subtype,
            exception = exception  
        };
    }
    public void LoadTestSet(int set)
    {
        AllPossibleTests.Clear();
        AllTestResults.Clear();

        switch (set)
        {
        case 0:
            AllPossibleTests.Add(new TemplateTest(this));
            AllPossibleTests.Add(new FlagshipScriptingTest(this));
            AllPossibleTests.Add(new ScriptingUDimAddEqualTest(this));
            AllPossibleTests.Add(new ScriptingUDimSubEqualTest(this));
            AllPossibleTests.Add(new ScriptingUDim2AddEqualTest(this));
            AllPossibleTests.Add(new ScriptingUDim2SubEqualTest(this));
            AllPossibleTests.Add(new ScriptingUDim2DoesntEqualUdimTest(this));
            AllPossibleTests.Add(new ScriptingUDimDoesntEqualColor3Test(this));
            AllPossibleTests.Add(new ScriptingBrickColorDoesntEqualColor3Test(this));
            AllPossibleTests.Add(new ScriptingBrickColorPalleteTest(this));
            AllPossibleTests.Add(new ScriptingInstanceMethodCachingTest(this));
            AllPossibleTests.Add(new ScriptingInstancePropertyGetTest(this));
            AllPossibleTests.Add(new ScriptingInstancePropertySetTest(this));
            AllPossibleTests.Add(new ScriptingInstanceMethodCallTest(this));
            AllPossibleTests.Add(new ScriptingInstanceMethodCallConsequencesTest(this));
            AllPossibleTests.Add(new ScriptingInstanceCreationTest(this));
            AllPossibleTests.Add(new ScriptingModuleScriptCreationTest(this));
            AllPossibleTests.Add(new ScriptingWaitingTest(this));
            break;
        }
    }
    public void RunAllTests()
    {
        GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_GameScript, "Creating RunAllTests task");

        GameSchedulerTask task = new GameSchedulerTask()
        {
            DebugName = "Run All Tests",
            GameScheduler = GameManager.GameScheduler,
            Delegate = _ =>
            {
                for (int i = 0; i < AllPossibleTests.Count; i++)
                {
                    AllPossibleTests[i].BeginTest();
                }
                return SchedulerTaskResult.CompletedSuccess;
            }
        };
        GameManager.GameScheduler.Schedule(task);

        GameManager.GameScheduler.EndTracedSecurityOverride();
    }
}