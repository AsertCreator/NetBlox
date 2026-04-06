using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
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
		public static bool FFlagLogAnchorFactorChanges = true;

		public PhysicsAssembly? Assembly;

		private object physicsRepresentationLock = new();
		public bool IsActuallyAnchored => 
			anchoredFactorUserChoice || anchoredFactorNonDomestic || isHumanoidAttachment || anchoredFactorWeldToAnchored;

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
				var og = IsActuallyAnchored;
				anchoredFactorUserChoice = value;
				if (FFlagLogAnchorFactorChanges)
					LogManager.LogInfo(GetFullName() + ": AnchoredFactorUserChoice = " + value);
				if (!GameManager.PhysicsManager.DisablePhysics && IsActuallyAnchored != og)
					ReevaluatePhysicsRepresentation();
			}
		}
		[NotReplicated]
		public bool AnchoredFactorNonDomestic
		{
			get => anchoredFactorNonDomestic;
			set
			{
				var og = IsActuallyAnchored;
				anchoredFactorNonDomestic = value;
				if (FFlagLogAnchorFactorChanges)
					LogManager.LogInfo(GetFullName() + ": AnchoredFactorNonDomestic = " + value);
				if (!GameManager.PhysicsManager.DisablePhysics && IsActuallyAnchored != og)
					ReevaluatePhysicsRepresentation();
			}
		}
		[NotReplicated]
		public bool AnchoredFactorWeldToAnchored
		{
			get => anchoredFactorWeldToAnchored;
			set
			{
				var og = IsActuallyAnchored;
				anchoredFactorWeldToAnchored = value;
				if (FFlagLogAnchorFactorChanges)
					LogManager.LogInfo(GetFullName() + ": AnchoredFactorWeldToAnchored = " + value);
				if (!GameManager.PhysicsManager.DisablePhysics && IsActuallyAnchored != og)
					ReevaluatePhysicsRepresentation();
			}
		}
		[NotReplicated]
		public bool IsHumanoidAttachment
		{
			get => isHumanoidAttachment;
			set
			{
				isHumanoidAttachment = value;
				if (FFlagLogAnchorFactorChanges)
					LogManager.LogInfo(GetFullName() + ": IsHumanoidAttachment = " + value);
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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					_position = value;

					var localsim = GameManager.PhysicsManager.LocalSimulation;

					CurrentRigidBody.Position = value;

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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					_rotation = rotq;

					CurrentRigidBody.Orientation = Raymath.QuaternionFromEuler(value.Z, value.Y, value.X);

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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					_rotation = value;

					CurrentRigidBody.Orientation = value;

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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					_size = value;

					var localsim = GameManager.PhysicsManager.LocalSimulation;

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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					LinearVelocity = value;

					CurrentRigidBody.ApplyImpulse((value - CurrentRigidBody.Velocity) * CurrentRigidBody.Mass, Position);
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
					if (float.IsNaN(value.X) || !float.IsFinite(value.X) ||
						float.IsNaN(value.Y) || !float.IsFinite(value.Y) ||
						float.IsNaN(value.Z) || !float.IsFinite(value.Z))
						return;
					RotationalVelocity = value;
					
					// can i jus not implement this please

					CurrentRigidBody.AngularVelocity = value;
				}
			}
		}
		[Lua([Security.Capability.None])]
		public CFrame CFrame
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => PartCFrame;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Position = value.Position;
				QuaternionRotation = value.Rotation;
			}
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
				IsHumanoidAttachment = value;
				isHumanoidLimb = value;
			}
		}

		[Lua([Security.Capability.None])]
		public LuaSignal Touched { get; private set; }

		public event EventHandler? OnNetworkOwnershipChanged;
		public event EventHandler? BeforePhysicsRepresentationChanged;
		public event EventHandler? AfterPhysicsRepresentationChanged;

		public RigidBody CurrentRigidBody;
		public RigidBodyShape CurrentShape;

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
		protected bool isHumanoidAttachment = false;
		protected bool anchoredFactorWeldToAnchored = false;

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

			CurrentRigidBody = GameManager.PhysicsManager.LocalSimulation.CreateRigidBody();

			Size = new Vector3(4, 1, 2);

			CurrentRigidBody.Position = Position;

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

				if (!isHumanoidAttachment)
				{
					if (IsActuallyAnchored)
						CurrentRigidBody.MotionType = MotionType.Static;
					else
						CurrentRigidBody.MotionType = MotionType.Dynamic;
				}
				else
					CurrentRigidBody.MotionType = MotionType.Kinematic;

				AfterPhysicsRepresentationChanged?.Invoke(this, new());
			}
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
				GameManager.PhysicsManager.Actors.Remove(this);
				if (Assembly == null)
					GameManager.PhysicsManager.LocalSimulation.Remove(CurrentRigidBody);
				else
					PhysicsAssembly.RemovePartFromAssembly(this);
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
		public void Reset()
		{
			/*
			 *
						   ≠=      √÷×≈           
				 ++++ +++++ ++++++++ ++++++++++++ 
				 ++++++++ ≈++-+---++++ ++++++∞+++ 
				 √+++÷    +++-+-+++÷+++   ≈+++++  
				  +π         ++=+-++         ++√  
				 +       ++≠  ++√+√      ++√  ++  
				 + ÷           + +       ++    += 
				 + +           + + +           +  
				 +  ∞         +  +  +       + ++  
				  ++  √+× ∞ ++  + +×  + ++√  ++   
					++++++++   ∞+   +++   +++ -   
				   ≠        ++ √+++×       ≈++    
				   +++++++++++ √++++++++++++++    
				   ++++++-++ ÷ ≈+  +++++  ≈÷=+    
					++ ≈+++-++ ++++ ++≈- + ++     
				  +-++π≠++  ++ ++++ ≠+=+=√++++    
				  +√≈+++++√+++ ++ ≠+= +∞ π∞  +    
						 ≈==≈÷ ++π++++++++++      
							   ++                 
									   π          
				   ++                     π++     
				   ∞ ∞   ++           +     +     
						   +              × √     
						 +++         ++           
						 √+          +            
                                  
			 */
		}

		protected virtual void OnSizeChanged(Vector3 newsize) { }
		protected virtual void OnPositionChanged(Vector3 newpos) { }
		protected virtual void OnRotationChanged(Quaternion q) { }
		protected virtual void OnSurfaceChanged() { }
	}
}
