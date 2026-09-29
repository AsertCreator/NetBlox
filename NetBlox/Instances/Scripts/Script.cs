using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts;

[Creatable]
public class Script : BaseScript
{
    public override string ClassName => nameof(Script);

    public Script(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        if (gameManager.NetworkMode == Network.NetworkMode.Server)
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
        if (className != nameof(Script))
            return base.IsA(className);
        return true;
    }
}