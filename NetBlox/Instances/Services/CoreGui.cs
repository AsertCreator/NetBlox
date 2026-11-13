using System.Diagnostics;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.UI;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
[ExplorerIconIndex(46)]
public class CoreGui : BasePlayerGui
{
    public override string ClassName => nameof(CoreGui);

    public const ulong NETWORK_CONSTANT_ID = 19;

    public CoreGui(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL3);
    }

    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();

        if (!GameManager.AccessFlags.HasFlag(AccessFlags.DisableCoreScripts))
        {
            GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_StarterScript, "Starting StarterScript");

            if (GameManager.NetworkMode == Network.NetworkMode.Server)
            {
                AssetDownloadTask? starterScript = GameManager.GameAssetManager.QuickLoad("rbxasset://scripts/ServerStarterScript.lua");
                if (starterScript == null)
                    throw new InvalidOperationException("ServerStarterScript couldn't be found");
                
                using Stream stream = starterScript.ReadAsset()!;
                using StreamReader streamReader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                ScriptContext context = Root.GetService<ScriptContext>();

                context.StartSourceScriptWithCurrentIdentity("ServerStarterScript", streamReader.ReadToEnd());
            }
            else if (GameManager.NetworkMode == Network.NetworkMode.Client)
            {
                AssetDownloadTask? starterScript = GameManager.GameAssetManager.QuickLoad("rbxasset://scripts/StarterScript.lua");
                if (starterScript == null)
                    throw new InvalidOperationException("StarterScript couldn't be found");
                
                using Stream stream = starterScript.ReadAsset()!;
                using StreamReader streamReader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                ScriptContext context = Root.GetService<ScriptContext>();

                context.StartSourceScriptWithCurrentIdentity("StarterScript", streamReader.ReadToEnd());
            }
            else
            {
                Trace.TraceWarning("Skipping StarterScript for the Neither network mode");
            }

            GameManager.GameScheduler.EndTracedSecurityOverride();
        }
        else
        {
            Trace.TraceInformation("Skipping StarterScript due to AccessFlags policy");
        }

        if (GameManager.PairedConsoleArguments.ContainsKey("Script") && GameManager.NetworkMode == Network.NetworkMode.Server)
        {
            string filePath = GameManager.PairedConsoleArguments["Script"];
            ScriptContext context = Root.GetService<ScriptContext>();

            GameSchedulerTask? task = context.StartSourceScriptWithCurrentIdentity("ServerScript", File.ReadAllText(filePath));
            if (task != null)
                task.Identity = SecurityIdentity.SI_StarterScript;
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void LoadModules()
    {
        Folder? modulesFolder = FindFirstChild("Modules") as Folder;

        if (modulesFolder != null)
        {
            Trace.TraceWarning("Modules are probably already loaded, skipping...");
            return;
        }

        string modulesDirectoryPath = Path.Combine(GameManager.GameAssetManager.ContentDirectory, "scripts/Modules");
        DirectoryInfo modulesDirectory = new DirectoryInfo(modulesDirectoryPath);

        if (!modulesDirectory.Exists)
        {
            Trace.TraceWarning("Modules are missing in the scripts subdirectory of the content directory, skipping...");
            return;
        }

        modulesFolder = GameManager.GameRegistry.Construct<Folder>();
        modulesFolder.Parent = this;
        modulesFolder.Name = "Modules";

        foreach (FileInfo scriptFileInfo in modulesDirectory.GetFiles())
        {
            if (scriptFileInfo.Name.EndsWith(".lua"))
            {
                ModuleScript moduleScript = GameManager.GameRegistry.Construct<ModuleScript>();
                moduleScript.Name = scriptFileInfo.Name.Substring(0, scriptFileInfo.Name.Length - 4);
                moduleScript.Source = File.ReadAllText(scriptFileInfo.FullName);
                moduleScript.Parent = modulesFolder;
            }
        }
    }

    public override Instance[] GetChildren()
    {
        ScriptSchedulerTask? scriptSchedulerTask = GameManager.GameScheduler.CurrentSchedulerTask as ScriptSchedulerTask;
        if (scriptSchedulerTask == null)
            return base.GetChildren();
        if (scriptSchedulerTask.Identity == null)
            return [];
        if (!scriptSchedulerTask.Identity.RequireSimpleCapability(SimpleSecurityCapabilityLevel.RobloxScript))
            return [];
        return base.GetChildren();
    }
    public override bool AskToBeParent(Instance child)
    {
        ScriptSchedulerTask? scriptSchedulerTask = GameManager.GameScheduler.CurrentSchedulerTask as ScriptSchedulerTask;
        if (scriptSchedulerTask == null)
            return true;
        if (scriptSchedulerTask.Identity == null)
            return false;
        if (!scriptSchedulerTask.Identity.RequireSimpleCapability(SimpleSecurityCapabilityLevel.RobloxScript))
            return false;
        return base.AskToBeParent(child);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL3 && GameManager.NetworkMode == Network.NetworkMode.Client && IsPartOfDataModel)
        {
            Render();
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
        if (className != nameof(CoreGui))
            return base.IsA(className);
        return true;
    }
}