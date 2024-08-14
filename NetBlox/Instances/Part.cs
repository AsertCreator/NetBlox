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
							new Color((byte)(Color3.R), (byte)(Color3.G), (byte)(Color3.B), Color3.A), Faces.Top, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R), (byte)(Color3.G), (byte)(Color3.B), Color3.A), Faces.Top);

					if (LeftSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.5f), (byte)(Color3.G * 0.5f), (byte)(Color3.B * 0.5f), Color3.A), Faces.Left, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.5f), (byte)(Color3.G * 0.5f), (byte)(Color3.B * 0.5f), Color3.A), Faces.Left);

					if (RightSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.4f), (byte)(Color3.G * 0.4f), (byte)(Color3.B * 0.4f), Color3.A), Faces.Right, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.4f), (byte)(Color3.G * 0.4f), (byte)(Color3.B * 0.4f), Color3.A), Faces.Right);

					if (BottomSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.2f), (byte)(Color3.G * 0.2f), (byte)(Color3.B * 0.2f), Color3.A), Faces.Bottom, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.2f), (byte)(Color3.G * 0.2f), (byte)(Color3.B * 0.2f), Color3.A), Faces.Bottom);

					if (FrontSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.4f), (byte)(Color3.G * 0.4f), (byte)(Color3.B * 0.4f), Color3.A), Faces.Front, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.4f), (byte)(Color3.G * 0.4f), (byte)(Color3.B * 0.4f), Color3.A), Faces.Front);

					if (BackSurface == SurfaceType.Studs)
						RenderUtils.DrawCubeTextureRec(st, Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.7f), (byte)(Color3.G * 0.7f), (byte)(Color3.B * 0.7f), Color3.A), Faces.Back, true);
					else
						RenderUtils.DrawCubeFaced(Position, Rotation, Size.X, Size.Y, Size.Z,
							new Color((byte)(Color3.R * 0.7f), (byte)(Color3.G * 0.7f), (byte)(Color3.B * 0.7f), Color3.A), Faces.Back);
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
