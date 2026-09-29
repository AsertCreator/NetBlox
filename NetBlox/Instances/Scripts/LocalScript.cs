using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts;

[Creatable]
public class LocalScript : BaseScript
{
    public override string ClassName => nameof(LocalScript);

    public LocalScript(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        if (gameManager.NetworkMode == Network.NetworkMode.Client)
            GameManager.TryGetEventForId(GameEvent.EVENT_HEARTBEAT)?.RegisterInstance(this);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
        if (args.GameEvent.Id == GameEvent.EVENT_HEARTBEAT)
        {
            if (GameManager.CurrentPhase < InitializationStage.Alive)
                return;
            ExecutionOpportunity(SecurityIdentity.SI_GameScript);
            GameManager.TryGetEventForId(GameEvent.EVENT_HEARTBEAT)?.UnregisterInstance(this);
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(LocalScript))
            return base.IsA(className);
        return true;
    }
}