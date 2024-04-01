using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	public class Part : BasePart
	{
		[Lua]
		[Replicated]
		public Shape Shape { get; set; } = Shape.Block;

		public override void Render()
		{
			switch (Shape)
			{
				case Shape.Ball:
					break;
				case Shape.Block:
					RenderUtils.DrawCubeTextureRec(RenderManager.StudTexture, Position, Size.X, Size.Y, Size.Z, Color, Faces.All, true);
					break;
				case Shape.Cylinder:
					break;
				case Shape.Wedge:
					break;
				case Shape.CornerWedge:
					break;
			}

			base.Render();
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Part) == classname) return true;
			return base.IsA(classname);
		}
	}
}
