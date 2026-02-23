using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;
using System.Runtime.CompilerServices;
using NetBlox.Instances.Services;

namespace NetBlox.Instances.Parts
{
	[Creatable]
	public class Part : BasePart
	{
		[Lua([Security.Capability.None])]
		public Shape Shape { get; set; } = Shape.Block;

		public Part(GameManager ins) : base(ins) { }

		private bool IsAbleToRender()
		{
			if (LocalLighing != null && LocalLighing.WasDestroyed)
				LocalLighing = null;

			if (LocalLighing == null)
			{
				LocalLighing = Root.GetService<Lighting>(true);
				return false;  // now parts REQUIRE Lighting service to be present in order to render (because sun)
			}

			if (IsCulled) return false;

			if (Transparency == 1) return false;

			if (!IsDescendantOfWorkspace()) return false;

			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe float ComputeBlockAFS(float nx, float ny, float nz)
		{
			return 1;

			float retu = 0;
			var og = new Vector3(nx, ny, nz);
			var ns = new Vector3(nx, ny, nz);
			var sun = LocalLighing.SunPosition;

			if (RenderCache.AFSCache == null)
			{
				RenderCache.AFSCache = [];
				RenderCache.DirtyCounter = 6;
			}

			if (LocalLighing.SunLocality)
				RenderCache.DirtyCounter = 6;

			if (RenderCache.DirtyCounter > 0)
			{
				if (FFlagShowAFSCacheReload)
					Raylib.DrawCubeWires(PartCFrame.Position, _size.X, _size.Y, _size.Z, Color.Red);

				if (LocalLighing!.SunLocality)
					ns += Position;

				Vector3 axis;
				float angle;
				Raymath.QuaternionToAxisAngle(PartCFrame.Rotation * RenderRotationOffset, &axis, &angle);

				ns = Raymath.Vector3RotateByAxisAngle(ns, axis, angle);

				var sl = sun.Length();
				var nl = ns.Length();
				var dot = Vector3.Dot(ns, sun);
				var res = MathF.Acos(dot / (nl * sl)) / MathF.PI;
				retu = 1 - res;
				RenderCache.AFSCache[og] = retu;
				RenderCache.DirtyCounter--;
			}
			else if (RenderCache.AFSCache != null)
				retu = RenderCache.AFSCache[og];
			return retu;
		}
		private void RenderAsBlock()
		{
			var st = GameManager.RenderManager.StudTexture;
			var bt = GameManager.RenderManager.BlankTexture;
			var tex = GameManager.RenderManager.StudTexture;

			if (LocalLighing.SunLocality)
				RenderCache.DirtyCounter = 6;

			if (TopSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var top = ComputeBlockAFS(0, 1, 0);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * top), (byte)(Color3.G * top), (byte)(Color3.B * top), Color3.A),
				Faces.Top, true);

			if (LeftSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var left = ComputeBlockAFS(-1, 0, 0);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * left), (byte)(Color3.G * left), (byte)(Color3.B * left), Color3.A),
				Faces.Left, true);

			if (RightSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var right = ComputeBlockAFS(1, 0, 0);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * right), (byte)(Color3.G * right), (byte)(Color3.B * right), Color3.A),
				Faces.Right, true);

			if (BottomSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var bottom = ComputeBlockAFS(0, -1, 0);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * bottom), (byte)(Color3.G * bottom), (byte)(Color3.B * bottom), Color3.A),
				Faces.Bottom, true);

			if (FrontSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var front = ComputeBlockAFS(0, 0, 1);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * front), (byte)(Color3.G * front), (byte)(Color3.B * front), Color3.A),
				Faces.Front, true);

			if (BackSurface == SurfaceType.Studs) tex = st;
			else tex = bt;
			var back = ComputeBlockAFS(0, 0, -1);
			RenderUtils.DrawCubeTextureRec(tex, PartCFrame.Position + RenderPositionOffset,
				PartCFrame.Rotation * RenderRotationOffset, Size.X, Size.Y, Size.Z,
				new Color(
					(byte)(Color3.R * back), (byte)(Color3.G * back), (byte)(Color3.B * back), Color3.A),
				Faces.Back, true);
		}
		public override void Render()
		{
			base.Render();

			if (!IsAbleToRender())
				return;

			switch (Shape)
			{
				case Shape.Ball:
					break;
				case Shape.Block:
					RenderAsBlock();
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
