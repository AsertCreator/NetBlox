using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;
using System.Runtime.CompilerServices;

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
					var bt = GameManager.RenderManager.BlankTexture;
					var tex = GameManager.RenderManager.StudTexture;
					var sun = new Vector3(2, 2, 0);

					[MethodImpl(MethodImplOptions.AggressiveInlining)]
					float AFS(float nx, float ny, float nz)
					{
						var ns = new Vector3(nx, ny, nz);
						ns = Raymath.Vector3RotateByAxisAngle(ns, Vector3.UnitX, Rotation.X / 180 * MathF.PI);
						ns = Raymath.Vector3RotateByAxisAngle(ns, Vector3.UnitY, Rotation.Y / 180 * MathF.PI);
						ns = Raymath.Vector3RotateByAxisAngle(ns, Vector3.UnitZ, Rotation.Z / 180 * MathF.PI);
						var sl = sun.Length();
						var nl = ns.Length();
						var dot = Vector3.Dot(ns, sun);
						var res = MathF.Acos(dot / (nl * sl)) / MathF.PI;
						return (1 - res);
					}

					if (TopSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var top = AFS(0, 1, 0);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * top), 
							(byte)(Color3.G * top), 
							(byte)(Color3.B * top), Color3.A), 
						Faces.Top, true);

					if (LeftSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var left = AFS(-1, 0, 0);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * left),
							(byte)(Color3.G * left),
							(byte)(Color3.B * left), Color3.A),
						Faces.Left, true);

					if (RightSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var right = AFS(1, 0, 0);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * right),
							(byte)(Color3.G * right),
							(byte)(Color3.B * right), Color3.A),
						Faces.Right, true);

					if (BottomSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var bottom = AFS(0, -1, 0);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * bottom),
							(byte)(Color3.G * bottom),
							(byte)(Color3.B * bottom), Color3.A),
						Faces.Bottom, true);

					if (FrontSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var front = AFS(0, 0, 1);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * front),
							(byte)(Color3.G * front),
							(byte)(Color3.B * front), Color3.A),
						Faces.Front, true);

					if (BackSurface == SurfaceType.Studs) tex = st;
					else tex = bt;
					var back = AFS(0, 0, -1);
					RenderUtils.DrawCubeTextureRec(tex, Position, Rotation, Size.X, Size.Y, Size.Z,
						new Color(
							(byte)(Color3.R * back),
							(byte)(Color3.G * back),
							(byte)(Color3.B * back), Color3.A),
						Faces.Back, true);

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
