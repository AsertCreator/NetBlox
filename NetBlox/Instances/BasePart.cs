using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	public class BasePart : PVInstance
	{
		[Lua]
		public bool Anchored { get; set; }
		[Lua]
		public bool Locked { get; set; }
		[Lua]
		public SurfaceType FrontSurface { get; set; }
		[Lua]
		public SurfaceType BackSurface { get; set; }
		[Lua]
		public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
		[Lua]
		public SurfaceType BottomSurface { get; set; }
		[Lua]
		public SurfaceType LeftSurface { get; set; }
		[Lua]
		public SurfaceType RightSurface { get; set; }
		[Lua]
		public Color Color { get; set; } = Color.Gray;
		[Lua]
		public Vector3 Position { get => Origin.Position; set => Origin.Position = value; }
		[Lua]
		public Vector3 Size { get; set; } = new Vector3(4, 1, 2);
		[Lua]
		public bool CanCollide { get; set; } = true;
		[Lua]
		public bool CanTouch { get; set; } = true;
		[Lua]
		public Vector3 Velocity { get; set; }
		public bool IsGrounded = false;

		public virtual void Render()
		{
			// render nothing
		}
		public override void Process()
		{
			base.Process();
		}
		public override bool IsA(string classname)
		{
			if (nameof(BasePart) == classname) return true;
			return base.IsA(classname);
		}
	}
}
