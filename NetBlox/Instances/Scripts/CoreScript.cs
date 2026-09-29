using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts;

[Creatable]
public class CoreScript : BaseScript
{
    public override string ClassName => nameof(CoreScript);

    public CoreScript(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        GameManager.TryGetEventForId(GameEvent.EVENT_HEARTBEAT)?.RegisterInstance(this);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
        if (args.GameEvent.Id == GameEvent.EVENT_HEARTBEAT)
        {
            if (GameManager.CurrentPhase < InitializationStage.Alive)
                return;
            ExecutionOpportunity(SecurityIdentity.SI_ElevatedGameScript);
            GameManager.TryGetEventForId(GameEvent.EVENT_HEARTBEAT)?.UnregisterInstance(this);
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(CoreScript))
            return base.IsA(className);
        return true;
    }
}