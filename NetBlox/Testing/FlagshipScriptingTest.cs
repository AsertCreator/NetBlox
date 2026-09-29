namespace NetBlox.Testing;

public class FlagshipScriptingTest : ScriptingTest
{
    public FlagshipScriptingTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    print(""FlagshipScriptingTest is running"")

    ";
}