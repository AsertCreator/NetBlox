using System.Diagnostics;
using NetBlox.Instances;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox;

public sealed class GameRenderer
{
    public readonly GameSchedulerTask RendererTask;
    public readonly GameManager GameManager;
    public DataModel Root => GameManager.RootModel;
    public RendererViewport CurrentViewport => rendererViewports[rendererViewportIndex];

    public FontSpecification DefaultFontSpecification;
    public FontRegistry FontRegistry;

    public bool IsDesktop = true;
    public bool DebugFlag = true;

    public string StatusText = "";

    public int PreferredFPS = 60;
    public int DpiAwareCellSize = 1;
    private Stopwatch RenderStopwatch = new();
    private List<RendererViewport> rendererViewports = [];
    private int rendererViewportIndex = 0;

    public GameRenderer(GameManager gameManager)
    {
        GameManager = gameManager;

        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.HighDpiWindow | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1600, 900, "NetBlox");

        GameManager.WindowReady = true;

        AssetDownloadTask? iconDownloadTask = GameManager.GameAssetManager.QuickLoad("rbxasset://textures/menu.png");
        if (iconDownloadTask != null)
        {
            Image iconImage = Raylib.LoadImage(iconDownloadTask.LocalDownloadPath);
            Raylib.SetWindowIcon(iconImage);
            Raylib.UnloadImage(iconImage);
        }

        DpiAwareCellSize = (int)Raylib.GetWindowScaleDPI().Y;

        DefaultFontSpecification.FontFilePath = GameManager.GameAssetManager.QuickLoad("rbxasset://fonts/arialbd.ttf")!.LocalDownloadPath!;
        DefaultFontSpecification.Size = 16;
        FontRegistry = new FontRegistry(this);

        rendererViewports.Add(new WorkspaceRendererViewport(this));
        rendererViewports.Add(new DebugRendererViewport(this));
        rendererViewports.Add(new UnitTestRendererViewport(this));
        rendererViewports.Add(new CoreGuiRendererViewport(this));

        GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_EngineRenderer, "Creating GameRenderer task");
        RendererTask = GameManager.GameScheduler.Schedule("GameRenderer", GameScheduler.SchedulerPhase.Rendering, thisJob =>
        {
            RenderStopwatch.Restart();

            if (!Raylib.WindowShouldClose())
            {
                GameManager.TryGetEventForId(GameEvent.EVENT_BEFORE_RENDER)?.Fire();

                if (Raylib.IsKeyPressed(KeyboardKey.F9))
                {
                    DebugFlag = !DebugFlag;
                }

                Raylib.BeginDrawing();

                if (rendererViewports.Count > 0)
                    rendererViewports[rendererViewportIndex].RenderFrame();

                Raylib.EndDrawing();

                if (Raylib.IsKeyPressed(KeyboardKey.P))
                {
                    if (++rendererViewportIndex >= rendererViewports.Count)
                        rendererViewportIndex = 0;
                }

                GameManager.TryGetEventForId(GameEvent.EVENT_AFTER_RENDER)?.Fire();
            }
            else
            {
                Raylib.CloseWindow();
                GameManager.Shutdown();
            }

            RenderStopwatch.Stop();

            if (!GameManager.GameScheduler.PreferUncapped)
                thisJob.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromSeconds(1 / (float)PreferredFPS) - RenderStopwatch.Elapsed);

            if (GameManager.ShuttingDown)
                return SchedulerTaskResult.CompletedSuccess;
            else
                return SchedulerTaskResult.NotCompleted;
        });
        GameManager.GameScheduler.EndTracedSecurityOverride();
    }
}