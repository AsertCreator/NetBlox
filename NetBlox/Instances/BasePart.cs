using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Instances;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances
{
	public class BasePart : PVInstance
	{
		[Lua]
		[Replicated]
		public bool Anchored { get; set; }
		[Lua]
		[Replicated]
		public bool Locked { get; set; }
		[Lua]
		[Replicated]
		public SurfaceType FrontSurface { get; set; }
		[Lua]
		[Replicated]
		public SurfaceType BackSurface { get; set; }
		[Lua]
		[Replicated]
		public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
		[Lua]
		[Replicated]
		public SurfaceType BottomSurface { get; set; }
		[Lua]
		[Replicated]
		public SurfaceType LeftSurface { get; set; }
		[Lua]
		[Replicated]
		public SurfaceType RightSurface { get; set; }
		[Lua]
		[Replicated]
		public Color Color { get; set; } = Color.Gray;
		[Lua]
		[Replicated]
		public Vector3 Position { get => Origin.Position; set => Origin.Position = value; }
		[Lua]
		[Replicated]
		public Vector3 Size { get; set; }
		[Lua]
		[Replicated]
		public bool CanCollide { get; set; } = true;
		[Lua]
		[Replicated]
		public bool CanTouch { get; set; } = true;
		[Lua]
		[Replicated]
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
