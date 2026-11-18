using System.Diagnostics;
using NetBlox.Instances.Parts;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances;

// synchronize all changes with Snap.cs & Glue.cs

[Creatable]
[ExplorerIconIndex(34)]
public class Weld : JointInstance
{
    protected InstanceID lastpart0;
    protected InstanceID lastpart1;

    public bool weldCreated;

    public override string ClassName => nameof(Weld);

    public Weld(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_BEFORE_PHYSICS);
        RegisterForEventId(GameEvent.EVENT_RENDERGIZMOS);
    }

    private void CreateWeld()
    {
        Debug.Assert(Part0 != null);
        Debug.Assert(Part1 != null);

        if (GameManager.PhysicsSolver == null)
        {
            Trace.TraceWarning("Cannot apply weld when GameManager.PhysicsSolver is null!");
            return;
        }

        Part0.JoinAssembliesWith(Part1);
        lastpart0 = Part0.InstanceID;
        lastpart1 = Part1.InstanceID;
        weldCreated = true;
        GameManager.PhysicsSolver?.FinalizeSetAnchoredForBasePart(Part0, Part0.Anchored);
    }
    private void DestroyWeld()
    {
        BasePart? oldPart0 = GameManager.GameRegistry.GetLocalInstanceById(lastpart0) as BasePart;
        BasePart? oldPart1 = GameManager.GameRegistry.GetLocalInstanceById(lastpart1) as BasePart;
        if (oldPart0 != null && oldPart1 != null)
            oldPart0.BreakAssembliesWith(oldPart1, this);
        weldCreated = false;
        lastpart0 = 0;
        lastpart1 = 0;
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGIZMOS)
        {
            if (Part0 != null && Part1 != null && weldCreated)
            {
                Rlgl.DisableDepthTest();
                Raylib.DrawCylinderEx(Part0.Position, Part1.Position, 0.13f, 0.13f, 3, new Color(60, 128, 60));
                Rlgl.EnableDepthTest();
            }
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS)
        {
            UpdateWeld();
        }
    }

    public void UpdateWeld()
    {
        BasePart? part0 = Part0;
        BasePart? part1 = Part1;

        if (part0 == null || part1 == null)
            return;

        if (weldCreated && (!IsPartOfDataModel || lastpart0 != part0.InstanceID || lastpart1 != part1.InstanceID))
            DestroyWeld();
        if (!weldCreated && IsPartOfDataModel)
            CreateWeld();
    }

    public void SetWeldCreated(bool weldCreated)
    {
        this.weldCreated = weldCreated;

        if (weldCreated)
        {
            if (Part0 != null)
                lastpart0 = Part0.InstanceID;
            else
                lastpart0 = 0;

            if (Part1 != null)
                lastpart1 = Part1.InstanceID;
            else
                lastpart1 = 0;
        }
        else
        {
            lastpart0 = 0;
            lastpart1 = 0;
        }
    }

    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();
        UpdateWeld();
    }

    public override void CommitStageDestroying()
    {
        base.CommitStageDestroying();
        DestroyWeld();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Weld))
            return base.IsA(className);
        return true;
    }
}