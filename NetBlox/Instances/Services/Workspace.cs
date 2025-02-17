using NetBlox.Runtime;
using System.Numerics;
using Raylib_cs;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using System.Runtime.CompilerServices;
using NetBlox.Rendering;

namespace NetBlox.Instances.Services
{
	public struct RaycastResult
	{
		public BasePart? Part;
		public float Distance;
		public Vector3 Normal;
		public Vector3 Where;
	}
	public struct RaycastRequest
	{
		public Vector3 From;
		public Vector3 To;
		public float MaxDistance;
		public BasePart? Ignore;
	}
	[Service]
	public class Workspace : Instance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public Instance? MainCamera { get; set; }
		[Lua([Security.Capability.None])]
		public float Gravity { get; set; } = -9.8f;
		[Lua([Security.Capability.None])]
		public float FallenPartsDestroyHeight { get; set; } = -50;
		[Lua([Security.Capability.None])]
		public bool BirdAmbient 
		{ 
			get => birdAmbient; 
			set 
			{
				if (Ambient.HasValue && GameManager.NetworkManager.IsClient && birdAmbient && !value)
					GameManager.RenderManager.StopSound(Ambient.Value);
				birdAmbient = value;
			} 
		}
		public SpawnLocation? SpawnLocation;
		public Sound? Ambient;
		private bool birdAmbient = true;

		public Workspace(GameManager ins) : base(ins) 
		{ 
			birdAmbient = true;
			if (GameManager.NetworkManager.IsClient)
			{
				RenderManager.LoadSound("rbxasset://sounds/birdsambient.mp3", x => Ambient = x);
			}
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Workspace) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void ZoomToExtents()
		{
			GameManager.RenderManager.MainCamera.Position = new Vector3(50, 40, 0);
			GameManager.RenderManager.MainCamera.Target = Vector3.Zero;
		}
		private class RayHitHandler : IRayHitHandler
		{
			public CollidableReference? Ignorable;
			public CollidableReference Reference;
			public float Distance;
			public Vector3 Normal;
			public Vector3 Where;
			public bool Found;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool AllowTest(CollidableReference collidable) => !Ignorable.HasValue || collidable != Ignorable.Value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool AllowTest(CollidableReference collidable, int childIndex) => true;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
			{
				Reference = collidable;
				Distance = t;
				Found = true;
				Normal = normal;
				Where = ray.Origin + (ray.Direction * t);
			}
		}
		public RaycastResult Raycast(RaycastRequest req)
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;
			var handler = new RayHitHandler();

			if (req.Ignore != null)
			{
				handler.Ignorable = req.Ignore.IsActuallyAnchored ? 
					new CollidableReference(req.Ignore.StaticHandle!.Value) :
					new CollidableReference(CollidableMobility.Dynamic, req.Ignore.BodyHandle!.Value);
			}

			sim.RayCast(req.From, Vector3.Normalize(req.To - req.From), req.MaxDistance, ref handler);

			if (!handler.Found)
				return new RaycastResult()
				{
					Distance = -1,
					Part = null!
				};

			var mobility = handler.Reference.Mobility;
			BasePart? basePart = null;

			if (mobility == CollidableMobility.Dynamic)
			{
				var body = handler.Reference.BodyHandle;
				var actors = GameManager.PhysicsManager.Actors;
				basePart = actors.Find(x => x.BodyHandle == body);
			}
			else
			{
				var stati = handler.Reference.StaticHandle;
				var actors = GameManager.PhysicsManager.Actors;
				basePart = actors.Find(x => x.StaticHandle == stati);
			}

			return new RaycastResult()
			{
				Distance = handler.Distance,
				Part = basePart,
				Where = handler.Where
			};
		}
		public override void Process()
		{
			base.Process();
			GameManager.PhysicsManager.Step();
			if (GameManager.NetworkManager.IsClient && BirdAmbient && Ambient.HasValue) 
			{
				if (!GameManager.RenderManager.IsSoundPlaying(Ambient.Value))
					GameManager.RenderManager.PlaySound(Ambient.Value);
			}
		}
		public void Render()
		{
		}
	}
}
