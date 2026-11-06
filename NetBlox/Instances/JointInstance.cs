using NetBlox.Instances.Parts;
using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances;

public class JointInstance : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CoordinateFrame C0 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CoordinateFrame C1 { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BasePart? Part0
    {
        get
        {
            if (cachedPart0 != null && cachedPart0.TryGetTarget(out BasePart? target))
                return target;
            BasePart? bp = GameManager.GameRegistry.GetLocalInstanceById(part0) as BasePart;
            if (bp == null)
                return null;
            if (cachedPart0 != null)
                cachedPart0.SetTarget(bp);
            else
                cachedPart0 = new WeakReference<BasePart?>(bp);
            return bp;
        }
        set
        {
            if (value != null && part0 != value.InstanceID)
            {
                BasePart? oldpart = Part0;
                oldpart?.joints.Remove(InstanceID);
            }

            if (value == null)
            {
                BasePart? oldpart = Part0;
                oldpart?.joints.Remove(InstanceID);
                part0 = 0;
            }
            else
            {
                part0 = value.InstanceID;
                if (cachedPart0 != null)
                    cachedPart0.SetTarget(value);
                else
                    cachedPart0 = new WeakReference<BasePart?>(value);
                Part0?.joints.Add(InstanceID);
            }
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BasePart? Part1
    {
        get
        {
            if (cachedPart1 != null && cachedPart1.TryGetTarget(out BasePart? target))
                return target;
            BasePart? bp = GameManager.GameRegistry.GetLocalInstanceById(part1) as BasePart;
            if (bp == null)
                return null;
            if (cachedPart1 != null)
                cachedPart1.SetTarget(bp);
            else
                cachedPart1 = new WeakReference<BasePart?>(bp);
            return bp;
        }
        set
        {
            if (value != null && part1 != value.InstanceID)
            {
                BasePart? oldpart = Part1;
                oldpart?.joints.Remove(InstanceID);
            }

            if (value == null)
            {
                BasePart? oldpart = Part1;
                oldpart?.joints.Remove(InstanceID);
                part1 = 0;
            }
            else
            {
                part1 = value.InstanceID;
                if (cachedPart1 != null)
                    cachedPart1.SetTarget(value);
                else
                    cachedPart1 = new WeakReference<BasePart?>(value);
                Part1?.joints.Add(InstanceID);
            }
        }
    }

    private WeakReference<BasePart?>? cachedPart0;
    private WeakReference<BasePart?>? cachedPart1;

    protected InstanceID part0;
    protected InstanceID part1;

    public override string ClassName => nameof(JointInstance);

    public JointInstance(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override void CommitStageDestroying()
    {
        base.CommitStageDestroying();
        Part0?.joints.Remove(InstanceID);
        Part1?.joints.Remove(InstanceID);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(JointInstance))
            return base.IsA(className);
        return true;
    }
}