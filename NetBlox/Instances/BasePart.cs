﻿using BepuPhysics;
using BepuPhysics.Collidables;
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
	public class BasePart : PVInstance, I3DRenderable
	{
		public bool IsActuallyAnchored => IsDomestic ? _anchored : true;
		[Lua([Security.Capability.None])]
		public bool Anchored
		{
			get => _anchored;
			set
			{
				var localsim = GameManager.PhysicsManager.LocalSimulation;

				_anchored = value;

				if (IsActuallyAnchored)
				{
					if (BodyHandle.HasValue)
					{
						localsim.Bodies.Remove(BodyHandle.Value);
						BodyHandle = null;
					}
					CreateStaticHandle();
				}
				else
				{
					if (StaticHandle.HasValue) 
					{ 
						localsim.Statics.Remove(StaticHandle.Value);
						StaticHandle = null;
					}
					CreateBodyHandle();
				}
			}
		}
		[Lua([Security.Capability.None])]
		public bool Locked { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType FrontSurface 
		{
			get => frontSurface;
			set
			{
				frontSurface = value;
				OnSurfaceChanged();
			}
		}
		[Lua([Security.Capability.None])]
		public SurfaceType BackSurface
		{
			get => backSurface;
			set
			{
				backSurface = value;
				OnSurfaceChanged();
			}
		}
		[Lua([Security.Capability.None])]
		public SurfaceType TopSurface
		{
			get => topSurface;
			set
			{
				topSurface = value;
				OnSurfaceChanged();
			}
		}
		[Lua([Security.Capability.None])]
		public SurfaceType BottomSurface
		{
			get => bottomSurface;
			set
			{
				bottomSurface = value;
				OnSurfaceChanged();
			}
		}
		[Lua([Security.Capability.None])]
		public SurfaceType LeftSurface
		{
			get => leftSurface;
			set
			{
				leftSurface = value;
				OnSurfaceChanged();
			}
		}
		[Lua([Security.Capability.None])]
		public SurfaceType RightSurface
		{
			get => rightSurface;
			set
			{
				rightSurface = value;
				OnSurfaceChanged();
			}
		}
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

				var localsim = GameManager.PhysicsManager.LocalSimulation;
				if (BodyHandle.HasValue)
					localsim.Bodies[BodyHandle.Value].Pose = _position;
				if (StaticHandle.HasValue)
					localsim.Statics[StaticHandle.Value].Pose = _position;

				OnPositionChanged(value);
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation
		{
			get => Raymath.QuaternionToEuler(_rotation) * new Vector3(180f / MathF.PI, 180f / MathF.PI, 180f / MathF.PI);
			set
			{
				var rotq = Raymath.QuaternionFromEuler(value.Z, value.Y, value.X);
				if (_rotation == rotq)
					return;
				_rotation = rotq;

				var localsim = GameManager.PhysicsManager.LocalSimulation;
				if (BodyHandle.HasValue)
					localsim.Bodies[BodyHandle.Value].Pose.Orientation = rotq;
				if (StaticHandle.HasValue)
					localsim.Statics[StaticHandle.Value].Pose.Orientation = rotq;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Size
		{
			get => _size;
			set
			{
				if (_size == value)
					return;
				_size = value;

				var localsim = GameManager.PhysicsManager.LocalSimulation;
				if (BodyHandle.HasValue) 
				{
					var idx = localsim.Bodies[BodyHandle.Value].Collidable.Shape;
					var box = localsim.Shapes.GetShape<Box>(idx.Index);

					localsim.Shapes.Remove(idx);

					box.Width = _size.X;
					box.Height = _size.Y;
					box.Length = _size.Z;

					idx = localsim.Shapes.Add(box);
					localsim.Bodies[BodyHandle.Value].Collidable.Shape = idx;
				}
				if (StaticHandle.HasValue)
				{
					var idx = localsim.Statics[StaticHandle.Value].Shape;
					var box = localsim.Shapes.GetShape<Box>(idx.Index);

					localsim.Shapes.Remove(idx);

					box.Width = _size.X;
					box.Height = _size.Y;
					box.Length = _size.Z;

					idx = localsim.Shapes.Add(box);
					localsim.Statics[StaticHandle.Value].SetShape(idx);
				}

				OnSizeChanged(value);
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

				var localsim = GameManager.PhysicsManager.LocalSimulation;
				if (BodyHandle.HasValue)
					localsim.Bodies[BodyHandle.Value].Velocity.Linear = _lastvelocity;
			}
		}
		[Lua([Security.Capability.None])]
		public CFrame CFrame
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => PartCFrame;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => PartCFrame = value;
		}
		public Vector3 _position
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => PartCFrame.Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				PartCFrame.Position = value;
				if (this is BasePart bp)
				{
					if (bp.LocalLighing != null && bp.LocalLighing.SunLocality)
						bp.RenderCache.DirtyCounter = 6;
				}
			}
		}
		public Quaternion _rotation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => PartCFrame.Rotation;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				PartCFrame.Rotation = value;
				if (this is BasePart bp)
					bp.RenderCache.DirtyCounter = 6;
			}
		}
		/// <summary>
		/// Use this if the part is anchored OR if its foreign (owned by another player)<br/>
		/// ========================================<br/>
		/// Use this if the part is server-side and its anchored
		/// </summary>
		public StaticHandle? StaticHandle;
		/// <summary>
		/// Use this if the part is NOT anchored AND its domestic (owned by us)<br/>
		/// ========================================<br/>
		/// Use this if the part is server-side and its NOT anchored
		/// </summary>
		public BodyHandle? BodyHandle;
		public PartRenderCache RenderCache = new();
		public Lighting? LocalLighing;
		public bool IsGrounded = false;
		public bool IsDirty = false;
		public CFrame PartCFrame;
		public Vector3 _size;
		public Vector3 _lastvelocity;
		public bool _anchored = false;
		public Vector3 _lastposition;
		public Quaternion _lastrotation;
		protected SurfaceType frontSurface;
		protected SurfaceType backSurface;
		protected SurfaceType topSurface = SurfaceType.Studs;
		protected SurfaceType bottomSurface;
		protected SurfaceType leftSurface;
		protected SurfaceType rightSurface;

		// they are internal as a workaround for serializationmanager
		internal Vector3 _physicsposition
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (_position != value)
					IsDirty = true;
				_position = value;
			}
		}
		internal Quaternion _physicsrotation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (_rotation != value)
					IsDirty = true;
				_rotation = value;
			}
		}
		internal Vector3 _physicsvelocity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (_lastvelocity != value)
					IsDirty = true;
				_lastvelocity = value;
			}
		}

		public BasePart(GameManager ins) : base(ins)
		{
			_size = new Vector3(4, 1, 2);
			_position = new Vector3(0, 0, 0);
			_rotation = default;
			_lastvelocity = new Vector3(0, 0, 0);

			IsDomestic = true;
			Anchored = false;

			GameManager.PhysicsManager.Actors.Add(this);
		}
		public override void PivotTo(CFrame pivot)
		{
			CFrame = pivot;
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
			GameManager.PhysicsManager.Actors.Remove(this);
			base.Destroy();
		}

		protected virtual void OnSizeChanged(Vector3 newsize) { }
		protected virtual void OnPositionChanged(Vector3 newpos) { }
		protected virtual void OnRotationChanged(Quaternion q) { }
		protected virtual void OnSurfaceChanged() { }
	}
}
