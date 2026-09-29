global using GlobalTimestamp = ulong;
global using InstanceID = ulong;

using System.Diagnostics;
using System.Numerics;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Instances.Services.Internal;
using NetBlox.Network;
using NetBlox.Physics;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox;

public sealed class GameManager
{
    public GameRegistry GameRegistry { get; private set; }
    public GameScheduler GameScheduler { get; private set; }
    public GameRenderer? GameRenderer { get; private set; }
    public PhysicsSolver? PhysicsSolver { get; private set; }
    public GameAssetManager GameAssetManager { get; private set; }
    public CloudConfiguration CloudConfiguration { get; private set; }
    public GameSchedulerTask? HeartbeatTask { get; private set; }
    public DataModel RootModel { get; private set; }
    public InitializationStage CurrentPhase { get; private set; } = InitializationStage.Newborn;
    public int HeartbeatRate = 30;
    public bool SchedulerRunning;
    public bool ScriptsRunning;
    public bool ShuttingDown;
    public readonly NetworkMode NetworkMode;
    public Stopwatch GlobalTime;
    public EditorMode EditorMode;
    public bool WindowReady;

    public Dictionary<string, string> PairedConsoleArguments = [];
    public List<string> UnpairedConsoleArguments = [];

    public PriorityQueue<InstanceID, GlobalTimestamp> DestructionTimetable;

    private Dictionary<string, GameEvent> allGameEvents = [];
    private Dictionary<string, Action> allVerbs = [];
    private Stopwatch heartbeatStopwatch = new();

    public GameManager(NetworkMode networkMode)
    {
        GameRegistry = new GameRegistry(this);
        GameScheduler = new GameScheduler(this);
        PhysicsSolver = new PhysicsSolver(this);
        GameAssetManager = new GameAssetManager(this, false);
        NetworkMode = networkMode;

        DestructionTimetable = new PriorityQueue<InstanceID, GlobalTimestamp>();
        GlobalTime = new Stopwatch();
        GlobalTime.Start();

        CreateGameEventNamed(GameEvent.EVENT_PLAYERADDED, "Player Added");
        CreateGameEventNamed(GameEvent.EVENT_PLAYERREMOVED, "Player Removed");
        CreateGameEventNamed(GameEvent.EVENT_LOCALPLAYERCHANGED, "Local Player Changed");

        CreateGameEventNamed(GameEvent.EVENT_HEARTBEAT, "Heartbeat - Process");
        CreateGameEventNamed(GameEvent.EVENT_RENDER3D, "Render 3D - Process");
        CreateGameEventNamed(GameEvent.EVENT_RENDERGUI_LEVEL0, "Render GUI - Level 0");
        CreateGameEventNamed(GameEvent.EVENT_RENDERGUI_LEVEL1, "Render GUI - Level 1");
        CreateGameEventNamed(GameEvent.EVENT_RENDERGUI_LEVEL2, "Render GUI - Level 2");
        CreateGameEventNamed(GameEvent.EVENT_RENDERGUI_LEVEL3, "Render GUI - Level 3");
        CreateGameEventNamed(GameEvent.EVENT_PHYSICS, "Physics - Process");

        CreateGameEventNamed(GameEvent.EVENT_LOG_INFO, "Lua Output - Info");
        CreateGameEventNamed(GameEvent.EVENT_LOG_WARN, "Lua Output - Warning");

        CreateGameEventNamed(GameEvent.EVENT_KICKED, "Got Kicked");
        CreateGameEventNamed(GameEvent.EVENT_NETWORKSERVER_STARTED, "Network Server - Started");
        CreateGameEventNamed(GameEvent.EVENT_NETWORKSERVER_STOPPED, "Network Server - Stopped");
        CreateGameEventNamed(GameEvent.EVENT_NETWORKCLIENT_STARTED, "Network Client - Started");
        CreateGameEventNamed(GameEvent.EVENT_NETWORKCLIENT_STOPPED, "Network Client - Stopped");

        RegisterVerb("shutdown", Shutdown);

        DataModel? dataModel = GameRegistry.TryCreateNewDomesticInstanceOfClass("DataModel") as DataModel;
        if (dataModel == null)
            throw new InvalidOperationException("GameRegistry.TryCreateNewDomesticInstanceOfClass doesn't return DataModel for DataModel");

        RootModel = dataModel;
        SchedulerRunning = false;
        ScriptsRunning = true;
        ShuttingDown = false;

        CloudConfiguration = new CloudConfiguration();

        RootModel.GetService<PlatformService>();
        RootModel.GetService<CoreGui>();
    }

    public void AddConsoleArguments(string[] strings)
    {
        try
        {
            for (int i = 0; i < strings.Length; i++)
            {
                string argument = strings[i];

                if (argument.StartsWith("-"))
                {
                    string nextargument = "";
                    int ogi = i;

                    if (strings.Length - 1 != i)
                        nextargument = strings[++i];

                    if (nextargument.StartsWith("-") || string.IsNullOrWhiteSpace(nextargument))
                    {
                        i = ogi;
                        UnpairedConsoleArguments.Add(argument.Substring(1));
                    }
                    else
                    {
                        PairedConsoleArguments[argument.Substring(1)] = nextargument;
                    }
                }
            }
        }
        catch
        {
            Trace.TraceWarning("Failed to parse console arguments; skipping the rest of them...");
        }
    }
    public void RegisterVerb(string id, Action action)
    {
        allVerbs[id] = action;
    }
    public bool TryInvokeVerb(string id)
    {
        if (allVerbs.TryGetValue(id, out Action? action))
        {
            action();
            return true;
        }
        return false;
    }

    public GlobalTimestamp CurrentGlobalTimestamp()
    {
        return (ulong)GlobalTime.Elapsed.TotalMicroseconds;
    }
    public GlobalTimestamp TimestampInTheFuture(TimeSpan timeSpan)
    {
        return (ulong)((GlobalTime.Elapsed + timeSpan).TotalMicroseconds);
    }

    public void InitializeRendering()
    {
        GameRenderer = new GameRenderer(this);
    }
    private SchedulerTaskResult DoHeartbeat(GameSchedulerTask heartbeatTask)
    {
        if (DestructionTimetable.Count > 0)
        {
            bool hadRemovedAnything = false;
            do
            {
                hadRemovedAnything = false;

                if (DestructionTimetable.Count <= 0)
                    break;

                GlobalTimestamp current = CurrentGlobalTimestamp();
                Instance? instance = GameRegistry.GetLocalInstanceById(DestructionTimetable.Peek());

                if (instance == null)
                    continue;

                if (instance.ShouldBeDestroyedBy <= current)
                {
                    DestructionTimetable.Dequeue();
                    instance.Destroy();
                    hadRemovedAnything = true;
                }
            }
            while (hadRemovedAnything && DestructionTimetable.Count > 0);
        }

        heartbeatStopwatch.Restart();
        LateInitializationAndActivation();
        PhysicsSolver?.Step();
        TryGetEventForId(GameEvent.EVENT_HEARTBEAT)?.Fire();
        heartbeatStopwatch.Stop();

        if (!GameScheduler.PreferUncapped)
            heartbeatTask.WaitingTimeTarget = TimestampInTheFuture(TimeSpan.FromSeconds(1 / 60f) - heartbeatStopwatch.Elapsed);

        if (ShuttingDown)
            return SchedulerTaskResult.CompletedSuccess;
        else
            return SchedulerTaskResult.NotCompleted;
    }
    public GameEvent CreateGameEventNamed(string eventId, string eventDebugName)
    {
        GameEvent gameEvent = new GameEvent()
        {
            GameManager = this,
            DebugName = eventDebugName,
            Id = eventId
        };
        allGameEvents[eventId] = gameEvent;
        return gameEvent;
    }
    public GameEvent? TryGetEventForId(string eventId)
    {
        allGameEvents.TryGetValue(eventId, out GameEvent? gameEvent);
        return gameEvent;
    }
    public void Shutdown()
    {
        Trace.TraceInformation("Stopping GameManager (" + NetworkMode + ")...");

        RootModel.Close();

        SchedulerRunning = false;
        ScriptsRunning = false;
        ShuttingDown = true;

        CurrentPhase = InitializationStage.Destroying;
    }
    public void LoadPlaceFromDefaults(int id)
    {
        switch (id)
        {
            case 0:
            {
                Workspace workspace = RootModel.GetService<Workspace>();

                Part baseplate = GameRegistry.Construct<Part>();
                baseplate.Parent = workspace;
                baseplate.Anchored = true;
                baseplate.Name = "Baseplate";
                baseplate.Size = new Vector3(2048, 1, 2048);
                baseplate.Position = new Vector3(0, -30, 0);

                Part part = GameRegistry.Construct<Part>();
                part.Parent = workspace;
                part.Anchored = false;
                part.Position = new Vector3(10, 6, 0);

                Script script = GameRegistry.Construct<Script>();
                script.Parent = workspace;
                script.Source = "print(42); printidentity(); print(math.sin(math.pi / 2)); local x = Vector3.new(6, 7, 0) + Vector3.new(1, 1, 1); x.Z = 483; print(x * 4);";

                break;
            }
        }
    }
    public void LoadPlaceFromFile(string path)
    {
        
    }
    public void LoadLoadingPlace()
    {
        
    }

    public void LateInitializationAndActivation()
    {
        if (ShuttingDown)
            return;

        try
        {
            Instance[]? allInstances = GameRegistry.FlushNewborns();

            if (allInstances == null)
                return;

            for (int i = 0; i < allInstances.Length; i++)
            {
                Instance instance = allInstances[i];
                if (instance.InitializationStage == InitializationStage.Newborn)
                    instance.CommitStageInitialize();
            }
            for (int i = 0; i < allInstances.Length; i++)
            {
                Instance instance = allInstances[i];
                if (instance.InitializationStage == InitializationStage.Newborn)
                    instance.CommitStageAlive();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("LateInitializationAndActivation failure: " + ex.GetType() + ", msg: " + ex.Message + "\n" + ex.StackTrace);
            Shutdown();
        }
    }

    public void BeginInitializationPhase()
    {
        // here we load stuff like coregui and rendering engine

        if (ShuttingDown)
            return;

        try
        {
            CurrentPhase = InitializationStage.Initializing;

            Instance[] allInstances = GameRegistry.SelectInstances(null);
            for (int i = 0; i < allInstances.Length; i++)
            {
                Instance instance = allInstances[i];
                instance.CommitStageInitialize();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("BeginInitializationPhase failure: " + ex.GetType() + ", msg: " + ex.Message + "\n" + ex.StackTrace);
            Shutdown();
        }
    }
    public void BeginAlivePhase()
    {
        if (ShuttingDown)
            return;

        try
        {
            SchedulerRunning = true;
            ScriptsRunning = true;

            CurrentPhase = InitializationStage.Alive;

            GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_EngineHeartbeat, "Creating Heartbeat task");
            HeartbeatTask = GameScheduler.Schedule("Heartbeat", GameScheduler.SchedulerPhase.Physics, DoHeartbeat);
            GameScheduler.EndTracedSecurityOverride();

            Instance[] allInstances = GameRegistry.SelectInstances(null);
            for (int i = 0; i < allInstances.Length; i++)
            {
                Instance instance = allInstances[i];
                instance.CommitStageAlive();
            }

            GameRegistry.ClearNewborns();
        }
        catch (Exception ex)
        {
            Trace.TraceError("BeginAlivePhase failure: " + ex.GetType() + ", msg: " + ex.Message + "\n" + ex.StackTrace);
            Shutdown();
        }
    }
}