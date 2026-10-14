using System.Diagnostics;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances;

// synchronize all changes with Weld.cs

[Creatable]
public class Snap : JointInstance
{
    protected InstanceID lastpart0;
    protected InstanceID lastpart1;

    private bool weldCreated;

    public override string ClassName => nameof(Snap);

    public Snap(ulong id, GameManager gameManager) : base(id, gameManager)
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
        weldCreated = true;
        GameManager.PhysicsSolver?.FinalizeSetAnchoredForBasePart(Part0, Part0.Anchored);
    }
    private void DestroyWeld()
    {
        if (Part0 != null && Part1 != null)
            Part0.BreakAssembliesWith(Part1);
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
            if (!weldCreated)
            {
                if (Part0 != null && Part1 != null)
                    CreateWeld();
            }
            else
            {
                if (Part0 == null || Part1 == null)
                    DestroyWeld();
            }
        }
    }

    public override void CommitStageDestroying()
    {
        base.CommitStageDestroying();
        DestroyWeld();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Snap))
            return base.IsA(className);
        return true;
    }
}