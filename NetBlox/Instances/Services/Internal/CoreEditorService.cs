using NetBlox.Editor;
using NetBlox.Structs;
using NetBlox.Runtime;
using System.Numerics;
using Raylib_cs;
using NetBlox.Rendering;

namespace NetBlox.Instances.Services.Internal;

[Service]
public class CoreEditorService : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool EditorModeEnabled { get; private set; }

    public override string ClassName => nameof(CoreEditorService);

    public Texture2D InstanceClassAtlas;
    public List<EditorWindow> EditorWindows;
    private bool everInitialized;

    public const ulong NETWORK_CONSTANT_ID = 104;

    public CoreEditorService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL4);
        EditorWindows = new List<EditorWindow>();
        
        gameManager.GameAssetManager.QuickLoad("rbxasset://textures/instanceIcons.png")?.AddCallbackForSuccess(x =>
        {
            InstanceClassAtlas = Raylib.LoadTexture(x.LocalDownloadPath);
        });
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void EnterEditorMode()
    {
        if (EditorModeEnabled)
            return;
        if (!everInitialized)
        {
            EditorWindows.Add(CreatePropertiesWindow());
            EditorWindows.Add(CreateExplorerWindow());
            EditorWindows.Add(CreateInstanceWindow());
            everInitialized = true;
        }
        EditorModeEnabled = true;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void LeaveEditorMode()
    {
        if (!EditorModeEnabled)
            return;
        EditorModeEnabled = false;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void ToggleEditorMode()
    {
        if (EditorModeEnabled)
            LeaveEditorMode();
        else
            EnterEditorMode();
    }

    public EditorWindow CreateExplorerWindow()
    {
        EditorWindow editorWindow = new EditorWindow();
        editorWindow.Position = new UDim2(0.8f, 0, 0, 0);
        editorWindow.Size = new UDim2(0.2f, 0, 1, 0);
        editorWindow.Title = "Explorer";

        EditorExplorerViewControl editorExplorerViewControl = new EditorExplorerViewControl();
        editorExplorerViewControl.Position = default;
        editorExplorerViewControl.Size = new UDim2(1, 0, 1, 0);
        editorWindow.Children.Add(editorExplorerViewControl);

        return editorWindow;
    }
    public EditorWindow CreatePropertiesWindow()
    {
        EditorWindow editorWindow = new EditorWindow();
        editorWindow.Position = default;
        editorWindow.Size = new UDim2(0.3f, 0, 1, 0);
        editorWindow.Title = "Properties";

        EditorPropertiesViewControl editorPropertiesViewControl = new EditorPropertiesViewControl();
        editorPropertiesViewControl.Position = default;
        editorPropertiesViewControl.Size = new UDim2(1, 0, 1, 0);
        editorWindow.Children.Add(editorPropertiesViewControl);

        return editorWindow;
    }
    public EditorWindow CreateInstanceWindow()
    {
        EditorWindow editorWindow = new EditorWindow();
        editorWindow.Position = new UDim2(0.5f, -150, 0.8f, 0);
        editorWindow.Size = new UDim2(0, 300, 0.2f, 0);
        editorWindow.Title = "Instance";

        EditorButtonControl destroyButton = new EditorButtonControl();
        destroyButton.Text = "Destroy";
        destroyButton.EnabledCallback = () =>
        {
            Instance[] instances = Root.GetService<Selection>().Get();
            return instances.Length > 0;
        };
        destroyButton.ClickedCallback = InvokeVerbDelete;
        destroyButton.Size = new UDim2(1, -6, 0, 22);
        destroyButton.Position = new UDim2(0, 3, 0, 3);

        editorWindow.Children.Add(destroyButton);

        return editorWindow;
    }

    public void InvokeVerbDelete()
    {
        Instance[] instances = Root.GetService<Selection>().Get();
        GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_StudioPlugin, "Destroying Selected Instances");
        try
        {
            for (int i = 0; i < instances.Length; i++)
                instances[i].Destroy();
        }
        catch
        {
        }
        GameManager.GameScheduler.EndTracedSecurityOverride();
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL4 && EditorModeEnabled && GameManager.GameRenderer != null)
        {
            Vector2 container = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            Color windowBackgroundColor = new Color(30, 35, 40, 150);

            FontSpecification fontSpecification = GameManager.GameRenderer.DefaultFontSpecification;
            Font font = GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);

            foreach (EditorWindow window in EditorWindows)
            {
                Vector2 position = window.Position.Resolve(container);
                Vector2 size = window.Size.Resolve(container);

                Raylib.DrawRectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y, windowBackgroundColor);
                Raylib.DrawTextEx(font, window.Title, position + new Vector2(3, 3), fontSpecification.Size, 0, Color.White);

                window.RenderWindow(this);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) ||
                Raylib.IsKeyPressed(KeyboardKey.Delete) && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
                InvokeVerbDelete();
        }
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(CoreEditorService))
            return base.IsA(className);
        return true;
    }
}