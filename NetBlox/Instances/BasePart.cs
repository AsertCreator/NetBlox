﻿using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	public class BasePart : PVInstance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public bool Anchored 
		{
			get => _anchored;
			set 
			{ 
				_anchored = value;
				if (Actor != null)
					Actor.Downdate();
			} 
		}
		[Lua([Security.Capability.None])]
		public bool Locked { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType FrontSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType BackSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
		[Lua([Security.Capability.None])]
		public SurfaceType BottomSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType LeftSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType RightSurface { get; set; }
		[Lua([Security.Capability.None])]
		public Color Color { get; set; } = Color.Gray;
		[Lua([Security.Capability.None])]
		public Vector3 Position 
		{ 
			get => _position;
			set
			{
				_position = value;
				if (Actor != null)
					Actor.Downdate(); // I REALLY DONT KNOW TO NAME THIS :P
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation { get; set; }
		[Lua([Security.Capability.None])]
		public Vector3 Size { get; set; } = new Vector3(4, 1, 2);
		[Lua([Security.Capability.None])]
		public Vector3 size { get => Size; set => Size = value; }
		[Lua([Security.Capability.None])]
		public bool CanCollide { get; set; } = true;
		[Lua([Security.Capability.None])]
		public bool CanTouch { get; set; } = true;
		[Lua([Security.Capability.None])]
		public Vector3 Velocity { get; set; }
		public Actor Actor;
		public bool IsGrounded = false;

		public BasePart(GameManager ins) : base(ins) 
		{
			Actor = new(this);
			Actor.Downdate();
		}

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
		public override void Destroy()
		{
			lock (Actor)
			{
				if (Actor != null)
				{
					Actor.Remove();
					Actor = null!;
				}
			}
			base.Destroy();
		}
	}
}
