using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using MoonSharp.Interpreter;
using NetBlox.Common;
using NetBlox.Instances.Services;
using NetBlox.Network;
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

		public PartRenderCache()
		{
			DirtyCounter = 6;
		}
	}
	public class BasePart : PVInstance, I3DRenderable
	{
		public static bool FFlagShowAFSCacheReload = false;
		public static bool FFlagShowPartOwnerhsip = false;
		public static bool FFlagShowPartGroundedness = false;

		public PhysicsAssembly? Assembly;

		private object physicsRepresentationLock = new();

		public bool IsActuallyAnchored => 
			anchoredFactorUserChoice || anchoredFactorNonDomestic || anchoredFactorHumanoidAttachment;

		[Lua([Security.Capability.None])]
		public bool Anchored
		{
			get => anchoredFactorUserChoice;
			set => AnchoredFactorUserChoice = value;
		}
		[NotReplicated]
		public bool AnchoredFactorUserChoice
		{
			get => anchoredFactorUserChoice;
			set
			{
				anchoredFactorUserChoice = value;
				if (!GameManager.PhysicsManager.DisablePhysics)
					ReevaluatePhysicsRepresentation();
			}
		}
		[NotReplicated]
		public bool AnchoredFactorNonDomestic
		{
			get => anchoredFactorNonDomestic;
			set
			{
				anchoredFactorNonDomestic = value;
				if (!GameManager.PhysicsManager.DisablePhysics)
					ReevaluatePhysicsRepresentation();
			}
		}
		[NotReplicated]
		public bool AnchoredFactorHumanoidAttachment
		{
			get => anchoredFactorHumanoidAttachment;
			set
			{
				anchoredFactorHumanoidAttachment = value;
				if (!GameManager.PhysicsManager.DisablePhysics)
					ReevaluatePhysicsRepresentation();
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
				lock (physicsRepresentationLock)
				{
					if (_position == value)
						return;
					_position = value;
					if (float.IsNaN(value.X) || !float.IsFinite(value.X))
						return;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue)
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						body.Pose.Position = _position;
						body.UpdateBounds();
					}
					if (StaticHandle.HasValue)
					{
						var stat = localsim.Statics[StaticHandle.Value];
						if (!stat.Exists)
							return;
						stat.Pose.Position = _position;
						stat.UpdateBounds();
					}

					OnPositionChanged(value);
				}
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation
		{
			get => Raymath.QuaternionToEuler(_rotation) * new Vector3(180f / MathF.PI, 180f / MathF.PI, 180f / MathF.PI);
			set
			{
				lock (physicsRepresentationLock)
				{
					var rotq = Raymath.QuaternionFromEuler(value.Z / 180f * MathF.PI, value.Y / 180f * MathF.PI, value.X / 180f * MathF.PI);
					if (_rotation == rotq)
						return;
					_rotation = rotq;
					if (float.IsNaN(value.X) || !float.IsFinite(value.X))
						return;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue)
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						body.Pose.Orientation = rotq;
						body.UpdateBounds();
					}
					if (StaticHandle.HasValue)
					{
						var stat = localsim.Statics[StaticHandle.Value];
						if (!stat.Exists)
							return;
						stat.Pose.Orientation = rotq;
						stat.UpdateBounds();
					}

					OnRotationChanged(rotq);
				}
			}
		}
		[NotReplicated]
		internal Quaternion QuaternionRotation
		{
			get => _rotation;
			set
			{
				lock (physicsRepresentationLock)
				{
					if (_rotation == value)
						return;
					if (value == default)
						value = Quaternion.Identity;
					_rotation = value;
					if (float.IsNaN(value.X) || !float.IsFinite(value.X))
						return;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue)
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						body.Pose.Orientation = value;
						body.UpdateBounds();
					}
					if (StaticHandle.HasValue)
					{
						var stat = localsim.Statics[StaticHandle.Value];
						if (!stat.Exists)
							return;
						stat.Pose.Orientation = value;
						stat.UpdateBounds();
					}

					OnRotationChanged(value);
				}
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Size
		{
			get => _size;
			set
			{
				lock (physicsRepresentationLock)
				{
					if (_size == value)
						return;
					_size = value;
					if (float.IsNaN(value.X) || !float.IsFinite(value.X))
						return;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue) 
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						var idx = body.Collidable.Shape;
						var box = localsim.Shapes.GetShape<Box>(idx.Index);

						localsim.Shapes.Remove(idx);

						box.Width = _size.X;
						box.Height = _size.Y;
						box.Length = _size.Z;

						idx = localsim.Shapes.Add(box);
						body.Collidable.Shape = idx;
					}
					if (StaticHandle.HasValue)
					{
						var stat = localsim.Statics[StaticHandle.Value];
						if (!stat.Exists)
							return;
						var idx = stat.Shape;
						var box = localsim.Shapes.GetShape<Box>(idx.Index);

						localsim.Shapes.Remove(idx);

						box.Width = _size.X;
						box.Height = _size.Y;
						box.Length = _size.Z;

						idx = localsim.Shapes.Add(box);
						stat.SetShape(idx);
					}

					OnSizeChanged(value);
				}
			}
		}
		[NotReplicated]
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
			get => LinearVelocity;
			set
			{
				lock (physicsRepresentationLock)
				{
					if (LinearVelocity == value)
						return;
					LinearVelocity = value;
					if (float.IsNaN(value.X) || !float.IsFinite(value.X))
						return;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue)
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						body.ApplyLinearImpulse(LinearVelocity - body.Velocity.Linear);
						body.Awake = true;
					}
				}
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 AngularVelocity
		{
			get => RotationalVelocity;
			set
			{
				lock (physicsRepresentationLock)
				{
					if (RotationalVelocity == value)
						return;
					RotationalVelocity = value;

					var localsim = GameManager.PhysicsManager.LocalSimulation;
					if (BodyHandle.HasValue)
					{
						var body = localsim.Bodies[BodyHandle.Value];
						if (!body.Exists)
							return;
						body.ApplyAngularImpulse(RotationalVelocity - body.Velocity.Angular);
						body.Awake = true;
					}
				}
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
				if (value == default)
					value = Quaternion.Identity;
				PartCFrame.Rotation = value;
				if (this is BasePart bp)
					bp.RenderCache.DirtyCounter = 6;
			}
		}
		public bool IsGrounded
		{
			get
			{
				if (isHumanoidLimb)
					return false;

				var bb = BoundingBox;
				var size = bb.Max - bb.Min;
				var center = bb.Min + size / 2;
				var lec = new Vector3(center.X, center.Y - size.Y / 2, center.Z);
				var letl = new Vector3(center.X + size.X / 2, center.Y - size.Y / 2, center.Z + size.Z / 2);
				var letr = new Vector3(center.X - size.X / 2, center.Y - size.Y / 2, center.Z + size.Z / 2);
				var lebl = new Vector3(center.X + size.X / 2, center.Y - size.Y / 2, center.Z - size.Z / 2);
				var lebr = new Vector3(center.X - size.X / 2, center.Y - size.Y / 2, center.Z - size.Z / 2);

				if (GameManager.PhysicsManager.QuickRaycast(lec, -Vector3.UnitY, size.Y / 2 + 2).IsAround(size.Y / 2, 1) ||
					GameManager.PhysicsManager.QuickRaycast(letl, -Vector3.UnitY, size.Y / 2 + 2).IsAround(size.Y / 2, 1) || 
					GameManager.PhysicsManager.QuickRaycast(letr, -Vector3.UnitY, size.Y / 2 + 2).IsAround(size.Y / 2, 1) ||
					GameManager.PhysicsManager.QuickRaycast(lebl, -Vector3.UnitY, size.Y / 2 + 2).IsAround(size.Y / 2, 1) ||
					GameManager.PhysicsManager.QuickRaycast(lebr, -Vector3.UnitY, size.Y / 2 + 2).IsAround(size.Y / 2, 1))
					return true;
				return false;
			}
		}
		public BoundingBox BoundingBox
		{
			get
			{
				var mat = Raymath.QuaternionToMatrix(QuaternionRotation);
				float hx = Size.X / 2, hy = Size.Y / 2, hz = Size.Z / 2;
				float ex = MathF.Abs(mat.M11) * hx + MathF.Abs(mat.M12) * hy + MathF.Abs(mat.M13) * hz;
				float ey = MathF.Abs(mat.M21) * hx + MathF.Abs(mat.M22) * hy + MathF.Abs(mat.M23) * hz;
				float ez = MathF.Abs(mat.M31) * hx + MathF.Abs(mat.M32) * hy + MathF.Abs(mat.M33) * hz;
				Vector3 extents = new Vector3(ex, ey, ez);
				return new BoundingBox(Position - extents, Position + extents);
			}
		}

		[NotReplicated]
		public bool IsDomestic 
		{
			get => isDomestic;
			set
			{
				AnchoredFactorNonDomestic = !value;
				isDomestic = value;
			}
		}
		[NotReplicated]
		public bool IsHumanoidLimb
		{
			get => isHumanoidLimb;
			set
			{
				AnchoredFactorHumanoidAttachment = value;
				isHumanoidLimb = value;
			}
		}

		[Lua([Security.Capability.None])]
		public LuaSignal Touched { get; private set; }

		public event EventHandler? OnNetworkOwnershipChanged;
		public event EventHandler? BeforePhysicsRepresentationChanged;
		public event EventHandler? AfterPhysicsRepresentationChanged;
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
		public bool IsDirty = false;
		public CFrame PartCFrame;
		public Vector3 _size = new Vector3(4, 1, 2);
		public Vector3 LinearVelocity;
		public Vector3 RotationalVelocity; // what
		public bool _anchored = false;
		public Vector3 RenderPositionOffset = default;
		public Quaternion RenderRotationOffset = Quaternion.Identity;
		public bool IsCulled = false;
		public RemoteClient? Owner;
		public List<BasePart> TouchingWith = [];
		public List<Constraint> ActiveConstraints = [];
		public HashSet<CollidablePair> currentPairs = [];
		public HashSet<CollidablePair> previousPairs = [];
		protected SurfaceType frontSurface;
		protected SurfaceType backSurface;
		protected SurfaceType topSurface = SurfaceType.Studs;
		protected SurfaceType bottomSurface;
		protected SurfaceType leftSurface;
		protected SurfaceType rightSurface;

		protected bool isDomestic = true;
		protected bool isHumanoidLimb = false;

		protected bool anchoredFactorUserChoice = false;
		protected bool anchoredFactorNonDomestic = false;
		protected bool anchoredFactorHumanoidAttachment = false;

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
				if (LinearVelocity != value)
					IsDirty = true;
				LinearVelocity = value;
			}
		}

		public BasePart(GameManager ins) : base(ins)
		{
			Touched = new LuaSignal(ins);

			PartCFrame = new CFrame(new Vector3());

			GameManager.PhysicsManager.Actors.Add(this);

			AppManager.FastFlags.TryGetValue("FFlagShowAFSCacheReload", out FFlagShowAFSCacheReload);
			AppManager.FastFlags.TryGetValue("FFlagShowPartOwnerhsip", out FFlagShowPartOwnerhsip);
			AppManager.FastFlags.TryGetValue("FFlagShowPartGroundedness", out FFlagShowPartGroundedness);

			if (!GameManager.PhysicsManager.DisablePhysics)
				ReevaluatePhysicsRepresentation();
		}
		public override void PivotTo(CFrame pivot)
		{
			CFrame = pivot;
		}
		public void ReevaluatePhysicsRepresentation()
		{
			if (GameManager.PhysicsManager.DisablePhysics)
				return;

			lock (physicsRepresentationLock)
			{
				BeforePhysicsRepresentationChanged?.Invoke(this, new());

				if (IsActuallyAnchored)
				{
					DestroyBodyHandle();
					CreateStaticHandle();
				}
				else
				{
					DestroyStaticHandle();
					CreateBodyHandle();
				}

				AfterPhysicsRepresentationChanged?.Invoke(this, new());
			}
		}
		public CollidableReference GetCollidableReference()
		{
			if (BodyHandle.HasValue)
				return new CollidableReference(CollidableMobility.Dynamic, BodyHandle.Value);
			else
				return new CollidableReference(StaticHandle.Value);
		}
		public void DestroyBodyHandle()
		{
			var localsim = GameManager.PhysicsManager.LocalSimulation;

			if (BodyHandle.HasValue)
			{
				if (!localsim.Bodies[BodyHandle.Value].Exists)
				{
					BodyHandle = null;
					return;
				}
				localsim.Bodies.Remove(BodyHandle.Value);
				BodyHandle = null;
			}
		}
		public void DestroyStaticHandle()
		{
			var localsim = GameManager.PhysicsManager.LocalSimulation;

			if (StaticHandle.HasValue)
			{
				if (!localsim.Statics[StaticHandle.Value].Exists)
				{
					StaticHandle = null;
					return;
				}
				localsim.Statics.Remove(StaticHandle.Value);
				StaticHandle = null;
			}
		}
		public void CreateBodyHandle()
		{
			if (IsActuallyAnchored)
				throw new InvalidOperationException("Cannot call CreateBodyHandle on BaseParts with anchor factors");

			var localsim = GameManager.PhysicsManager.LocalSimulation;

			var collidable = new Box(_size.X, _size.Y, _size.Z);
			var inertia = collidable.ComputeInertia(1);
			var rotation = Raymath.QuaternionFromEuler(_rotation.X, _rotation.Y, _rotation.Z);
			var rigidpose = new RigidPose(_position, rotation);
			var index = localsim.Shapes.Add(collidable);
			var description = BodyDescription.CreateDynamic(rigidpose, inertia, index, 0.01f);
			description.Velocity.Linear = LinearVelocity;

			BodyHandle = localsim.Bodies.Add(description);
			GameManager.PhysicsManager.Collidable2BasePartMap[GetCollidableReference().Packed] = this;
		}
		public void CreateStaticHandle()
		{
			if (!IsActuallyAnchored)
				throw new InvalidOperationException("Cannot call CreateStaticHandle on BaseParts without anchor factors");

			var localsim = GameManager.PhysicsManager.LocalSimulation;

			var collidable = new Box(_size.X, _size.Y, _size.Z);
			var rotation = Raymath.QuaternionFromEuler(_rotation.X, _rotation.Y, _rotation.Z);
			var index = localsim.Shapes.Add(collidable);
			var description = new StaticDescription(_position, rotation, index);

			StaticHandle = localsim.Statics.Add(description);
			GameManager.PhysicsManager.Collidable2BasePartMap[GetCollidableReference().Packed] = this;
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

			if (IsGrounded && FFlagShowPartGroundedness)
				Raylib.DrawCube(PartCFrame.Position, Size.X, Size.Y, Size.Z, Color.Red);
			if (IsDomestic && FFlagShowPartOwnerhsip)
				Raylib.DrawCubeWires(PartCFrame.Position, Size.X, Size.Y, Size.Z, Color.Blue);
		}
		public override void Process() => base.Process();
		public override bool IsA(string classname) => nameof(BasePart) == classname || base.IsA(classname);
		public override void Destroy()
		{
			base.Destroy();

			lock (physicsRepresentationLock)
			{
				GameManager.PhysicsManager.Collidable2BasePartMap.Remove(GetCollidableReference().Packed);
				GameManager.PhysicsManager.Actors.Remove(this);
				if (BodyHandle.HasValue)
					GameManager.PhysicsManager.LocalSimulation.Bodies.Remove(BodyHandle.Value);
				if (StaticHandle.HasValue)
					GameManager.PhysicsManager.LocalSimulation.Statics.Remove(StaticHandle.Value);
			}
		}
		public void InvokeChangeNetworkOwnership() => OnNetworkOwnershipChanged?.Invoke(this, new());

		[Lua([Security.Capability.None])]
		public virtual void SetNetworkOwner(Player player)
		{
			if (!GameManager.NetworkManager.IsServer)
				throw new ScriptRuntimeException("Cannot call SetNetworkOwner on client!");

			if (player == null)
			{
				if (Owner == null)
					return;

				var aanchor = IsActuallyAnchored;
				IsDomestic = true;
				if (IsActuallyAnchored != aanchor)
					Anchored = Anchored;

				Owner.SendPacket(NPUpdatePlayerOwnership.Create(this, false));
				Owner = null;
			}
			else
			{
				RemoteClient client = player.Client;

				var aanchor = IsActuallyAnchored;
				IsDomestic = false;
				if (IsActuallyAnchored != aanchor)
					Anchored = Anchored;

				if (Owner != null)
					Owner.SendPacket(NPUpdatePlayerOwnership.Create(this, false));

				Owner = client;
				Owner.SendPacket(NPUpdatePlayerOwnership.Create(this, true));
			}
		}
		public void AddTouchingPart(BasePart basePart)
		{
			if (!TouchingWith.Contains(basePart))
			{
				TouchingWith.Add(basePart);
				Touched.Fire(LuaRuntime.PushInstance(basePart));
			}
		}
		public void RemoveTouchingPart(BasePart basePart)
		{
			if (TouchingWith.Contains(basePart))
			{
				TouchingWith.Remove(basePart);
				// Touched.Fire(LuaRuntime.PushInstance(basePart));
			}
		}
		public void AddCollidablePair(CollidablePair pair) => currentPairs.Add(pair);
		public void Reset()
		{
			try 
			{ 
				foreach (var pair in currentPairs) // is this the only foreach in this whole thing
				{
					if (!previousPairs.Contains(pair))
					{
						BasePart bpa = GameManager.PhysicsManager.Collidable2BasePartMap[pair.A.Packed];
						BasePart bpb = GameManager.PhysicsManager.Collidable2BasePartMap[pair.B.Packed];
						if (bpa == this)
							AddTouchingPart(bpb);
						else
							AddTouchingPart(bpa);
					}
				}
				foreach (var pair in previousPairs)
				{
					if (!currentPairs.Contains(pair))
					{
						BasePart bpa = GameManager.PhysicsManager.Collidable2BasePartMap[pair.A.Packed];
						BasePart bpb = GameManager.PhysicsManager.Collidable2BasePartMap[pair.B.Packed];
						if (bpa == this)
							RemoveTouchingPart(bpb);
						else
							RemoveTouchingPart(bpa);
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.LogError("Failed to comprehend collisions: " + ex.GetType() + ", msg: " + ex.Message);
			}

			var a = previousPairs;
			previousPairs = currentPairs;
			currentPairs = a;
			currentPairs.Clear();
		}

		protected virtual void OnSizeChanged(Vector3 newsize) { }
		protected virtual void OnPositionChanged(Vector3 newpos) { }
		protected virtual void OnRotationChanged(Quaternion q) { }
		protected virtual void OnSurfaceChanged() { }
	}
}
