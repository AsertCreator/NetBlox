using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Part : BasePart
	{
		[Lua([Security.Capability.None])]
		public Shape Shape { get; set; } = Shape.Block;

		public Part(GameManager ins) : base(ins) { }

		public override void Render()
		{
			switch (Shape)
			{
				case Shape.Ball:
					break;
				case Shape.Block:
					var st = GameManager.RenderManager.StudTexture;

					if (TopSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Top, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Top);

					if (LeftSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Left, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Left);

					if (RightSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Right, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Right);

					if (BottomSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Bottom, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z, 
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Bottom);

					if (FrontSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Front, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Front);

					if (BackSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Back, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color.R), (byte)(Color.G), (byte)(Color.B), Color.A), Faces.Back);
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
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Part) == classname) return true;
			return base.IsA(classname);
		}
	}
}
