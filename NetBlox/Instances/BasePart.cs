using BepuPhysics;
using BepuPhysics.Collidables;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances
{
	public struct PartRenderCache
	{
		public int DirtyCounter;
		public Dictionary<Vector3, float>? AFSCache;
	}
	public class BasePart : PVInstance, I3DRenderable
	{
		public bool IsActuallyAnchored => IsDomestic ? _anchored : true;
		[Lua([Security.Capability.None])]
		public bool Anchored
		{
			get => _anchored;
			set
			{
				if (_anchored != value)
				{
					var localsim = GameManager.PhysicsManager.LocalSimulation;

					if (_anchored)
					{
						localsim.Bodies.Remove(BodyHandle);
						CreateStaticHandle();
					}
					else
					{
						localsim.Statics.Remove(StaticHandle);
						CreateBodyHandle();
					}

					_anchored = value;
				}
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
			get => _position;
			set
			{
				if (_position == value)
					return;
				_position = value;
				// TODO: this
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation
		{
			get => _rotation;
			set
			{
				if (_rotation == value)
					return;
				_rotation = value;
				// TODO: this
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Size
		{
			get => _size;
			set
			{
				var localsim = GameManager.PhysicsManager.LocalSimulation;
				_size = value;

				if (_anchored)
				{

				}
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
			get => _lastvelocity;
			set
			{
				if (_lastvelocity == value)
					return;
				_lastvelocity = value;
				// TODO: this
			}
		}
		/// <summary>
		/// Use this if the part is anchored OR if its foreign (owned by another player)<br/>
		/// ========================================<br/>
		/// Use this if the part is server-side and its anchored
		/// </summary>
		public StaticHandle StaticHandle;
		/// <summary>
		/// Use this if the part is NOT anchored AND its domestic (owned by us)<br/>
		/// ========================================<br/>
		/// Use this if the part is server-side and its NOT anchored
		/// </summary>
		public BodyHandle BodyHandle;
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

				CreateBodyHandle();
			}
			GameManager.PhysicsManager.Actors.Add(this);
		}
		public void CreateBodyHandle()
		{
			if (_anchored)
				throw new InvalidOperationException("Cannot call CreateBodyHandle on anchored BaseParts");

			var localsim = GameManager.PhysicsManager.LocalSimulation;

			var collidable = new Box(_size.X, _size.Y, _size.Z);
			var inertia = collidable.ComputeInertia(1);
			var rotation = Raymath.QuaternionFromEuler(_rotation.X, _rotation.Y, _rotation.Z);
			var rigidpose = new RigidPose(_position, rotation);
			var index = localsim.Shapes.Add(collidable);
			var description = BodyDescription.CreateDynamic(rigidpose, inertia, index, 0.01f);
			description.Velocity.Linear = _lastvelocity;

			BodyHandle = localsim.Bodies.Add(description);
		}
		public void CreateStaticHandle()
		{
			// not necessarily, we might be a foreign part
			// if (_anchored)
			// 	throw new InvalidOperationException("Cannot call CreateBodyHandle on anchored BaseParts");

			var localsim = GameManager.PhysicsManager.LocalSimulation;

			var collidable = new Box(_size.X, _size.Y, _size.Z);
			var rotation = Raymath.QuaternionFromEuler(_rotation.X, _rotation.Y, _rotation.Z);
			var index = localsim.Shapes.Add(collidable);
			var description = new StaticDescription(_position, rotation, index);

			StaticHandle = localsim.Statics.Add(description);
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
			// TODO: this
			base.Destroy();
		}
		public override void SetPivot(CFrame pivot)
		{
			Position = pivot.Position;
			Rotation = pivot.Rotation;
			PivotOffset = default;
		}
	}
}
