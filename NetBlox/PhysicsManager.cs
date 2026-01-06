using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetBlox
{
	public class PhysicsManager
	{
		public GameManager GameManager;
		public Workspace? Workspace => GameManager.CurrentRoot.GetService<Workspace>(true);
		public float Gravity = -19.8f;
		public Simulation LocalSimulation;
		public BufferPool LocalSimulationBuffer;
		public ThreadDispatcher? DefaultThreadDispatcher;
		public List<BasePart> Actors = new();
		public Dictionary<uint, BasePart> Collidable2BasePartMap = [];
		public bool DisablePhysics = true; // not now
		internal Stopwatch physicsStopwatch = new();

		public PhysicsManager(GameManager gameManager)
		{
			var core = new CoreCallbacks(gameManager);
			var solver = new SolveDescription(8, 1);

			GameManager = gameManager;

			LocalSimulationBuffer = new BufferPool();
			LocalSimulation = Simulation.Create(LocalSimulationBuffer, core, core, solver);
		}
		public void SpringUpPhysics()
		{
			DisablePhysics = false;
			Actors.ForEach(x => x.ReevaluatePhysicsRepresentation());
		}
		public void ClientStep()
		{
			if (Workspace == null || DisablePhysics)
				return;

			var work = Workspace;

			LocalSimulation.Timestep(1.0f / AppManager.PreferredFPS, DefaultThreadDispatcher);

			for (int i = 0; i < Actors.Count; i++)
			{
				var box = Actors[i];

				if (!box.Anchored && box.IsDomestic) // if part is dynamic AND its domestic
				{
					if (!box.BodyHandle.HasValue)
						continue;

					// reflect this in rendering
					var refer = LocalSimulation.Bodies[box.BodyHandle.Value];

					if (box.IsDescendantOf(work))
					{
						box._physicsposition = refer.Pose.Position;
						box._physicsrotation = refer.Pose.Orientation;
						box._physicsvelocity = refer.Velocity.Linear;

						box.Reset();

						if (box._position.Y <= work.FallenPartsDestroyHeight)
						{
							box.Destroy();
							continue;
						}

						if (box.IsDirty)
						{
							GameManager.NetworkManager.SendServerboundPacket(NPPhysicsReplication.Create(box));
							box.IsDirty = false;
						}
					}
				}
			}
		}
		public void ServerStep()
		{
			if (Workspace == null || DisablePhysics)
				return;

			var work = Workspace;

			LocalSimulation.Timestep(1.0f / AppManager.PreferredFPS, DefaultThreadDispatcher);

			var clients = GameManager.NetworkManager.Clients;

			for (int i = 0; i < Actors.Count; i++)
			{
				var box = Actors[i];

				if (!box.Anchored && box.IsDomestic) // if part is dynamic AND its server-side
				{
					// reflect this in rendering
					if (!box.BodyHandle.HasValue)
						continue;

					var refer = LocalSimulation.Bodies[box.BodyHandle.Value];

					if (box.IsDescendantOf(work))
					{
						box._physicsposition = refer.Pose.Position;
						box._physicsrotation = refer.Pose.Orientation;
						box._physicsvelocity = refer.Velocity.Linear;

						if (box._position.Y <= work.FallenPartsDestroyHeight)
						{
							box.Destroy();
							continue;
						}

						var packet = NPPhysicsReplication.Create(box);

						if (box.IsDirty)
						{
							for (int j = 0; j < clients.Count; j++)
							{
								var client = clients[j];

								if (!GameManager.NetworkManager.ClientsReadyForReplication.Contains(client))
									continue;

								client.SendPacket(packet);
							}
						}
					}
					else
					{
						refer.Awake = false;
					}
				}
			}
		}
		public void Step()
		{
			try
			{
				GameManager.CurrentRunService.PreSimulation.Fire(DynValue.NewNumber(physicsStopwatch.Elapsed.TotalSeconds));
				physicsStopwatch.Reset();
				physicsStopwatch.Start();

				if (!DisablePhysics)
				{
					if (GameManager.NetworkManager.IsServer)
						ServerStep();
					else if (GameManager.NetworkManager.IsClient)
						ClientStep();
				}

				physicsStopwatch.Stop();

				GameManager.CurrentRunService.PostSimulation.Fire(DynValue.NewNumber(physicsStopwatch.Elapsed.TotalSeconds));
			}
			catch (Exception e)
			{
				LogManager.LogError("Physics solver had failed! " + e.GetType() + ", msg:" + e.Message);
				LogManager.LogError(e.StackTrace ?? "no stacktrace");
				// GameManager.Shutdown();
			}
		}
		public float QuickRaycast(Vector3 from, Vector3 direction, float max)
		{
			var raycast = new RayHitHandler();
			raycast.Distance = -1;
			LocalSimulation.RayCast(from, direction, max, ref raycast);
			return raycast.Found ? -1 : raycast.Distance;
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
	}
	internal struct CoreCallbacks : INarrowPhaseCallbacks, IPoseIntegratorCallbacks
	{
		public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
		public bool AllowSubstepsForUnconstrainedBodies => false;
		public bool IntegrateVelocityForKinematics => false;
		internal GameManager GameManager;
		internal Vector3Wide gravityWideDt;

		public CoreCallbacks(GameManager gm)
		{
			GameManager = gm;
		}

		public void Initialize(Simulation simulation)
		{
		}
		public void PrepareForIntegration(float dt)
		{
			gravityWideDt = Vector3Wide.Broadcast(new Vector3(0, GameManager.PhysicsManager.Gravity, 0) * dt);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
			BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
			ref BodyVelocityWide velocity)
		{
			velocity.Linear += gravityWideDt;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
			ref float speculativeMargin)
		{
			bool yeah = a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
			if (yeah)
			{
				var p0 = GameManager.PhysicsManager.Collidable2BasePartMap[a.Packed];
				var p1 = GameManager.PhysicsManager.Collidable2BasePartMap[b.Packed];
				return p0.CanCollide && p1.CanCollide;
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
		{
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
			out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
		{
			pairMaterial.FrictionCoefficient = 2f;
			pairMaterial.MaximumRecoveryVelocity = 2f;
			pairMaterial.SpringSettings = new SpringSettings(20, 0.8f);

			if (!GameManager.PhysicsManager.Collidable2BasePartMap.TryGetValue(pair.A.Packed, out var p0))
				return false;
			if (!GameManager.PhysicsManager.Collidable2BasePartMap.TryGetValue(pair.B.Packed, out var p1))
				return false;

			var humanoid = p0.GetHumanoidForInstance() ?? p1.GetHumanoidForInstance();
			if (humanoid != null)
			{
				pairMaterial.FrictionCoefficient = 0.4f;
				pairMaterial.MaximumRecoveryVelocity = 0.02f;
				pairMaterial.SpringSettings = new SpringSettings(2, 0.3f);
			}

			if (manifold.Count > 0)
			{
				p0.AddCollidablePair(pair);
				p1.AddCollidablePair(pair);
			}

			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, 
			ref ConvexContactManifold manifold)
		{
			return true;
		}
		public void Dispose()
		{
		}
	}
}
