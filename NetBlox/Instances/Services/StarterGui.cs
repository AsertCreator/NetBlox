using NetBlox.Instances.UI;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class StarterGui : BasePlayerGui
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool ShowDevelopmentGui { get; set; } = true;

    public override string ClassName => nameof(StarterGui);

    public const ulong NETWORK_CONSTANT_ID = 17;

    public StarterGui(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL1);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
        
        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL1 && ShowDevelopmentGui)
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
        if (className != nameof(StarterGui))
            return base.IsA(className);
        return true;
    }
}