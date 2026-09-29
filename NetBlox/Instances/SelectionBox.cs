using System.Numerics;
using NetBlox.Instances.Parts;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances;

[Creatable]
public class SelectionBox : InstanceAdornment
{
	[ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
	public bool Enabled { get; set; } = true;
	[ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
	public Color3 SurfaceColor3 { get; set; } = Color.SkyBlue;
	[ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
	public float SurfaceTransparency { get; set; } = 1;

    public override string ClassName => nameof(SelectionBox);

    public SelectionBox(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDER3D);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(SelectionBox))
            return base.IsA(className);
        return true;
    }
    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        BasePart? factualAdornee = Adornee;
        if (factualAdornee == null)
            factualAdornee = Parent as BasePart;

        if (args.GameEvent.Id == GameEvent.EVENT_RENDER3D && Enabled && factualAdornee != null)
        {
            Vector3 fpos = factualAdornee.Position;
            Vector3 fsize = factualAdornee.Size;

            Raylib.DrawCubeWires(fpos, fsize.X, fsize.Y, fsize.Z, SurfaceColor3);
			Raylib.DrawCube(fpos, fsize.X, fsize.Y, fsize.Z, new Color(SurfaceColor3.R, SurfaceColor3.G, SurfaceColor3.B, 
                unchecked((byte)((1 - SurfaceTransparency) * 255))));
        }
    }
}