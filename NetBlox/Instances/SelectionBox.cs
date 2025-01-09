using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances
{
	[Creatable]
	public class SelectionBox : InstanceAdornment, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;
		[Lua([Security.Capability.None])]
		public Color SurfaceColor3 { get; set; } = Color.SkyBlue;
		[Lua([Security.Capability.None])]
		public float SurfaceTransparency { get; set; } = 1;

		public SelectionBox(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(SelectionBox) == classname) return true;
			return base.IsA(classname);
		}
		public override void Decorate(BasePart part)
		{
			var size = part._size;
			var position = part._position;

			Raylib.DrawCubeWires(position, size.X, size.Y, size.Z, SurfaceColor3);
			Raylib.DrawCube(position, size.X, size.Y, size.Z, new Color(SurfaceColor3.R, SurfaceColor3.G, SurfaceColor3.B, unchecked((byte)((1 - SurfaceTransparency) * 255))));
		}
	}
}
