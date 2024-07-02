using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Qu3e;
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

				if (!_anchored)
					Body.Flags &= ~BodyFlags.eStatic;
				else
					Body.Flags &= ~BodyFlags.eDynamic;
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
				if (Body != null)
					Body.SetTransform(value);
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
		public bool IsGrounded = false;
		public BoxDef BoxDef;
		public Body Body;
		public Box Box;

		public BasePart(GameManager ins) : base(ins) 
		{
			Scene sc = Root.GetService<Workspace>().Scene;
			BodyDef bodyDef = new BodyDef();
			bodyDef.position.Set(Position.X, Position.Y, Position.Z);
			if (!Anchored)
				bodyDef.bodyType = BodyType.eDynamicBody;
			else
				bodyDef.bodyType = BodyType.eStaticBody;
			Body body = sc.CreateBody(bodyDef);
			BoxDef = new BoxDef();
			BoxDef.Set(Transform.Identity, Size);
			Box = body.AddBox(BoxDef);
			Body = body;

			GameManager.PhysicsManager.Actors.Add(this);
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
			if (Body != null)
			{
				GameManager.PhysicsManager.Actors.Remove(this);
				Scene sc = GameManager.CurrentRoot.GetService<Workspace>().Scene;
				lock (sc)
				{
					sc.RemoveBody(Body);
					Body = null!;
					Box = null!;
				}
			}
			base.Destroy();
		}
	}
}
