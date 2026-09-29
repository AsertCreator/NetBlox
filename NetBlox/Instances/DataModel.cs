using System.Diagnostics;
using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Instances.Services.Internal;
using NetBlox.Runtime;

namespace NetBlox.Instances;

public class DataModel : ServiceProvider
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Workspace? Workspace => FindService<Workspace>();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public RunService? RunService => FindService<RunService>();
    
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public long CreatorId { get; set; } = 1;
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public long GameId { get; set; } = 1;
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public long PlaceId { get; set; } = 1;
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public long PlaceVersion { get; set; } = 1;
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public long AuthorId { get; set; } = 1;
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public string JobId { get; set; } = "";
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser, SetValueLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public string UniverseName { get; set; } = "";

    public override Instance? Parent
    {
        get => null;
        set
        {
            if (value != null)
                throw new InvalidOperationException("DataModel cannot be parented");
        }
    }

    public override string ClassName => nameof(DataModel);

    public List<DynValue> boundOnClose = [];

    public const ulong NETWORK_CONSTANT_ID = 1;

    public DataModel(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    private int tempNumber;

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public int ReturnTwo()
    {
        return 2;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public int GetTempNumber()
    {
        return tempNumber;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void SetTempNumber(int number)
    {
        tempNumber = number;
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void Clear()
    {
        Root.RunService?.Stop();
        Root.FindService<Workspace>()?.Destroy();
        Root.FindService<Lighting>()?.Destroy();
        Root.FindService<Players>()?.Destroy();
        Root.FindService<RunService>()?.DestroyAllChildren();
        Root.FindService<ScriptContext>()?.DestroyAllChildren();
        Root.FindService<ReplicatedStorage>()?.Destroy();
        Root.FindService<ReplicatedFirst>()?.Destroy();
        Root.FindService<ServerScriptService>()?.Destroy();
        Root.FindService<ServerStorage>()?.Destroy();
        Root.FindService<StarterGui>()?.Destroy();
        Root.FindService<StarterPack>()?.Destroy();
        Root.FindService<StarterPlayer>()?.Destroy();
        Root.FindService<Teams>()?.Destroy();
        Root.FindService<Chat>()?.Destroy();
        Root.FindService<CoreGui>()?.Destroy();
        Root.FindService<PlatformService>()?.DestroyAllChildren();

        for (int i = 0; i < GameManager.GameScheduler.SchedulerTasks.Count; i++)
        {
            GameSchedulerTask task = GameManager.GameScheduler.SchedulerTasks[i];
            if (task is ScriptSchedulerTask)
            {
                GameManager.GameScheduler.SchedulerTasks.RemoveAt(i);
                i--;
            }
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void Load(string path)
    {
        SchedulerTaskResult DoLoad(GameSchedulerTask task)
        {
            string? localFilePath = task.UserData as string;
            if (localFilePath == null)
                throw new Exception("How is task.UserData not a string");
            
            Clear();

            PlaceParser placeParser = new PlaceParser(GameManager);
            placeParser.LoadPlaceMultiplexed(localFilePath);

            return SchedulerTaskResult.CompletedSuccess;
        }

        GameManager.GameAssetManager.QuickLoad(path)?
            .AddCallbackForFailure(x =>
            {
                Trace.TraceError("Failed to load place: couldn't fetch place");
            })
            .AddCallbackForSuccess(x =>
            {
                string? path = x.LocalDownloadPath;
                GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_RemoteServerControl, "Loading a place");
                GameManager.GameScheduler.Schedule("DataModel.Load", GameScheduler.SchedulerPhase.Any, DoLoad).UserData = path;
                GameManager.GameScheduler.EndTracedSecurityOverride();
            });
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void BindToClose(DynValue function)
    {
        DynValue dynValue = function.CheckType("DataModel:BindToClose", DataType.Function, flags: TypeValidationFlags.None);
        if (!boundOnClose.Contains(dynValue))
            boundOnClose.Add(dynValue);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool DefineFastFlag(string name, bool defaultValue)
    {
        if (!GameManager.CloudConfiguration.HasDefined(name))
            GameManager.CloudConfiguration.AddOverride(name, defaultValue);
        return GameManager.CloudConfiguration.GetFeatureFlagStatusBy(name, defaultValue);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public long DefineFastInt(string name, long defaultValue)
    {   
        if (!GameManager.CloudConfiguration.HasDefined(name))
            GameManager.CloudConfiguration.AddOverride(name, defaultValue);
        return GameManager.CloudConfiguration.GetFeatureFlagIntegerStatusBy(name, defaultValue);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string DefineFastString(string name, string defaultValue)
    {
        if (!GameManager.CloudConfiguration.HasDefined(name))
            GameManager.CloudConfiguration.AddOverride(name, defaultValue);
        return GameManager.CloudConfiguration.GetFeatureFlagStringStatusBy(name, defaultValue);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool GetFastFlag(string name)
    {
        return GameManager.CloudConfiguration.GetFeatureFlagStatusBy(name, false);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public long GetFastInt(string name)
    {
        return GameManager.CloudConfiguration.GetFeatureFlagIntegerStatusBy(name, 0);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string GetFastString(string name)
    {
        return GameManager.CloudConfiguration.GetFeatureFlagStringStatusBy(name, "");
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public DynValue ReadAllConsoleArguments()
    {
        SecurityIdentity? securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity();
        if (securityIdentity == null)
            return DynValue.Nil;

        ScriptContext scriptContext = GetService<ScriptContext>();
        Script script = scriptContext.GetLuaStateFor(securityIdentity);
        Table pairedtable = new Table(script);
        Table unpairedtable = new Table(script);
        Table completetable = new Table(script);

        foreach (KeyValuePair<string, string> pair in GameManager.PairedConsoleArguments)
            pairedtable[pair.Key] = pair.Value;
        
        foreach (string argument in GameManager.UnpairedConsoleArguments)
            unpairedtable[argument] = true;
        
        completetable["paired"] = pairedtable;
        completetable["unpaired"] = unpairedtable;

        return DynValue.NewTable(completetable);
    }

    public void Close()
    {
        try
        {
            for (int i = 0; i < boundOnClose.Count; i++)
            {
                Closure closure = boundOnClose[i].Function;
                closure.OwnerScript.CallWithTimeout(boundOnClose[i], 30000);
            }
        }
        catch (TimeoutException)
        {
            Trace.TraceError("Ignoring BindToClose callbacks due to them taking longer than 30 second to finish...");
        }

        GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_ElevatedStudioPlugin, "Closing the DataModel");

        ClearAllChildren();

        GameManager.GameScheduler.EndTracedSecurityOverride();
    }

    public override void Destroy()
    {
        Trace.TraceWarning("Attempted to destroy DataModel!");
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void Shutdown()
    {
        GameManager.Shutdown();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(DataModel))
            return base.IsA(className);
        return true;
    }
}