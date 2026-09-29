using System.Numerics;
using NetBlox.Testing;
using Raylib_cs;

namespace NetBlox.Rendering;

public class UnitTestRendererViewport : WorkspaceRendererViewport
{
    public GameTestManager GameTestManager;

    public UnitTestRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
        GameTestManager = new GameTestManager(gameRenderer.GameManager);
        GameTestManager.LoadTestSet(0);
    }

    public override string ConstructDebugString()
    {
        return base.ConstructDebugString() + ", UNIT TESTING MODE";
    }
    public override void RenderDebugInfo()
    {
        base.RenderDebugInfo();
        Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);

        Raylib.DrawTextEx(font, "Total test count: " + GameTestManager.AllPossibleTests.Count + ", press H to start all tests!", 
            new Vector2(50, 50), GameRenderer.DefaultFontSpecification.Size, 0, Color.White);

        for (int i = 0; i < GameTestManager.AllPossibleTests.Count; i++)
        {
            BaseTest test = GameTestManager.AllPossibleTests[i];
            if (!GameTestManager.AllTestResults.TryGetValue(test, out TestResult testResult))
            {
                Raylib.DrawTextEx(font, i + ": " + test + " - no result", 
                    new Vector2(50, 50 + 18 * (i + 1)), GameRenderer.DefaultFontSpecification.Size, 0, Color.DarkGray);
            }
            else
            {
                if (testResult.success)
                {
                    Raylib.DrawTextEx(font, i + ": " + test + " - SUCCESS", 
                        new Vector2(50, 50 + 18 * (i + 1)), GameRenderer.DefaultFontSpecification.Size, 0, Color.Green);
                }
                else
                {
                    Raylib.DrawTextEx(font, i + ": " + test + " - FAILURE - " + testResult.subtype, 
                        new Vector2(50, 50 + 18 * (i + 1)), GameRenderer.DefaultFontSpecification.Size, 0, Color.Red);
                }
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.H))
        {
            GameTestManager.LoadTestSet(0);
            GameTestManager.RunAllTests();
        }
    }
}