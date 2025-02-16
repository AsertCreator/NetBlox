using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox.Instances
{
	public struct PartRenderCache
	{
		public int DirtyCounter;
		public Dictionary<Vector3, float>? AFSCache;
	}
	public class BasePart : PVInstance, IPhysicsActor, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public bool Anchored
		{
			get => _anchored;
			set => _anchored = value;
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
		public Color Color3 { get; set; } = Color.Gray;
		[Lua([Security.Capability.None])]
		public BrickColor BrickColor
		{
			get => _brickColor;
			set
			{
				_brickColor = value;
				Color3 = _brickColor.Color;
			}
		}
		private BrickColor _brickColor;
		[Lua([Security.Capability.None])]
		public Vector3 Position
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position;
			set
			{
				if (_position == value)
					return;
				_position = value;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _rotation;
			set
			{
				if (_rotation == value)
					return;
				_rotation = value;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _size;
			set
			{
				if (_size == value)
					return;
				_size = value;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 size { get => Size; set => Size = value; }
		[Lua([Security.Capability.None])]
		public bool CanCollide { get; set; } = true;
		[Lua([Security.Capability.None])]
		public bool CanTouch { get; set; } = true;
		[Lua([Security.Capability.None])]
		public double Transparency { get; set; } = 0;
		[Lua([Security.Capability.None])]
		public Vector3 Velocity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _lastvelocity;
			set
			{
				if (_lastvelocity == value)
					return;
				_lastvelocity = value;
			}
		}
		public Vector3 BodyPosition 
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Position = value; 
		}
		public Vector3 BodySize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Size;
			set => Size = value;
		}
		public Vector3 BodyRotation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Rotation;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Rotation = value;
		}

		public PartRenderCache RenderCache = new();
		public Lighting? LocalLighing;
		public bool IsGrounded = false;
		public Vector3 _size;
		public Vector3 _lastvelocity;

		public BasePart(GameManager ins) : base(ins)
		{
			if (GameManager.NetworkManager.IsServer) // we are in server
			{
				// by default we ARE server-side AND unanchored
				_anchored = false;
				_size = new Vector3(1, 1, 1);
				_position = new Vector3(0, 0, 0);
				_rotation = new Vector3(0, 0, 0);
				_lastvelocity = new Vector3(0, 0, 0);
			}
			GameManager.PhysicsManager.Actors.Add(this);
		}
		public virtual void Render()
		{
			if (LocalLighing != null && LocalLighing.WasDestroyed)
				LocalLighing = null;

			if (LocalLighing == null)
			{
				LocalLighing = Root.GetService<Lighting>(true);
				return;  // now parts REQUIRE Lighting service to be present in order to render (because sun)
			}
		}
		public override void Process() => base.Process();
		public override bool IsA(string classname) => nameof(BasePart) == classname || base.IsA(classname);
		public override void Destroy()
		{
			GameManager.PhysicsManager.Actors.Remove(this);
			base.Destroy();
		}
		public override void SetPivot(CFrame pivot)
		{
			Position = pivot.Position;
			Rotation = pivot.Rotation;
			PivotOffset = default;
		}

		public void ReportContactBegin() => throw new NotImplementedException();
		public void ReportContactEnd() => throw new NotImplementedException();
	}
}
