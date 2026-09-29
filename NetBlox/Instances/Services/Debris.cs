using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
public class Debris : Instance
{
    public override string ClassName => nameof(Debris);

    public const ulong NETWORK_CONSTANT_ID = 12;

    public Debris(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void AddItem(Instance instance, double secondsLeft)
    {
        GlobalTimestamp deadline = GameManager.TimestampInTheFuture(TimeSpan.FromSeconds(secondsLeft));
        instance.ShouldBeDestroyedBy = deadline;
        GameManager.DestructionTimetable.Enqueue(instance.InstanceID, deadline);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Debris))
            return base.IsA(className);
        return true;
    }
}