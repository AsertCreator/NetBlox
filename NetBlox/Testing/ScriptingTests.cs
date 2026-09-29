using NetBlox.Runtime;

namespace NetBlox.Testing;

public class ScriptingUDimAddEqualTest : ScriptingTest
{
    public ScriptingUDimAddEqualTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(UDim.new(8, 9) + UDim.new(10, 93) == UDim.new(18, 102))

    ";
}
public class ScriptingUDimSubEqualTest : ScriptingTest
{
    public ScriptingUDimSubEqualTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(UDim.new(8, 93) - UDim.new(10, 3) == UDim.new(-2, 90))

    ";
}
public class ScriptingUDim2AddEqualTest : ScriptingTest
{
    public ScriptingUDim2AddEqualTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(UDim2.new(8, 9, 1, 39) + UDim2.new(10, 93, 0, 2) == UDim2.new(18, 102, 1, 41))

    ";
}
public class ScriptingUDim2SubEqualTest : ScriptingTest
{
    public ScriptingUDim2SubEqualTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(UDim2.new(8, 93, 99, 99) - UDim2.new(10, 3, 90, 93) == UDim2.new(-2, 90, 9, 6))

    ";
}
public class ScriptingUDim2DoesntEqualUdimTest : ScriptingTest
{
    public ScriptingUDim2DoesntEqualUdimTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(UDim2.new(8, 93, 99, 99) ~= UDim.new(-2, 90))

    ";
}
public class ScriptingUDimDoesntEqualColor3Test : ScriptingTest
{
    public ScriptingUDimDoesntEqualColor3Test(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(Color3.new(0.3, 0.4, 0.8) ~= UDim.new(-2, 90))

    ";
}
public class ScriptingBrickColorDoesntEqualColor3Test : ScriptingTest
{
    public ScriptingBrickColorDoesntEqualColor3Test(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(Color3.new(0.3, 0.4, 0.8) ~= BrickColor.new(194))

    ";
}
public class ScriptingBrickColorPalleteTest : ScriptingTest
{
    public ScriptingBrickColorPalleteTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(BrickColor.pallete(123) == BrickColor.new(194))

    ";
}
public class ScriptingInstanceMethodCachingTest : ScriptingTest
{
    public ScriptingInstanceMethodCachingTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(game.Destroy == game.Destroy)

    ";
}
public class ScriptingInstancePropertyGetTest : ScriptingTest
{
    public ScriptingInstancePropertyGetTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(game.JobId == game.JobId)

    ";
}
public class ScriptingInstancePropertySetTest : ScriptingTest
{
    public ScriptingInstancePropertySetTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    local archivable = game.Archivable;
    game.Archivable = not archivable;
    assert(game.Archivable ~= archivable);

    ";
}
public class ScriptingInstanceMethodCallTest : ScriptingTest
{
    public ScriptingInstanceMethodCallTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    assert(game:ReturnTwo() + 2 == 4)

    ";
}
public class ScriptingInstanceMethodCallConsequencesTest : ScriptingTest
{
    public ScriptingInstanceMethodCallConsequencesTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    local mynumber = 42;
    game:SetTempNumber(mynumber);
    assert(game:GetTempNumber() == mynumber);
    game:SetTempNumber(mynumber + 4);
    assert(game:GetTempNumber() ~= mynumber);

    ";
}
public class ScriptingInstanceCreationTest : ScriptingTest
{
    public ScriptingInstanceCreationTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    local moduleScript0 = Instance.new(""ModuleScript"");
    local moduleScript1 = Instance.new(""ModuleScript"");

    assert(moduleScript0 ~= moduleScript1);
    assert(moduleScript0.Parent == moduleScript1.Parent);

    ";
}
public class ScriptingModuleScriptCreationTest : ScriptingTest
{
    public ScriptingModuleScriptCreationTest(GameTestManager environment) : base(environment) { }

    public override SecurityIdentity ConductTestUnderIdentity() => SecurityIdentity.SI_StarterScript;

    public override string ScriptContent() => @"

    local moduleScript0 = Instance.new(""ModuleScript"");
    local moduleScript1 = Instance.new(""ModuleScript"");
    
    moduleScript0.Source = ""return {}"";
    moduleScript1.Source = ""return 42"";

    local module0 = require(moduleScript0);
    local module1 = require(moduleScript1);

    module0.li = ""lu"";

    assert(require(moduleScript0).li == ""lu"");
    assert(module1 + 50 == 92);

    ";
}
public class ScriptingWaitingTest : ScriptingTest
{
    public ScriptingWaitingTest(GameTestManager environment) : base(environment) { }

    public override string ScriptContent() => @"

    local counter = 0;
    wait(1)
    counter = counter + 70;
    wait(1)
    counter = counter - 35
    wait(1)
    counter = counter + 20
    assert(counter == 55)

    ";
}