namespace NetBlox.Testing;

public class FlagshipScriptingFailureTest : ScriptingTest
{
    public FlagshipScriptingFailureTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    error(""FlagshipScriptingFailureTest is running"")
    
    ";
}