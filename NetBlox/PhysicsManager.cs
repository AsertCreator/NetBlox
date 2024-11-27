using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox
{
	public class PhysicsManager
	{
		public GameManager GameManager;
		public Workspace? Workspace => (Workspace?)GameManager.CurrentRoot.GetService<Workspace>(true);
		public float Gravity 
		{ 
			get => (Workspace ?? throw new ScriptRuntimeException("No workspace is loaded")).Gravity; 
			set => (Workspace ?? throw new ScriptRuntimeException("No workspace is loaded")).Gravity = value; 
		}
		public Simulation LocalSimulation;
		public BufferPool LocalSimulationBuffer;
		public ThreadDispatcher DefaultThreadDispatcher;
		public List<BasePart> Actors = new();
		public bool DisablePhysics = true; // not now

		public PhysicsManager(GameManager gameManager)
		{
			var inpc = new InternalNarrowPhaseCallbacks();
			var ipic = new InternalPoseIntegratorCallbacks(new Vector3(0, -10, 0));
			var solver = new SolveDescription(8, 1);

			GameManager = gameManager;
			DefaultThreadDispatcher = new ThreadDispatcher(2);
			LocalSimulationBuffer = new BufferPool();
			LocalSimulation = Simulation.Create(LocalSimulationBuffer, inpc, ipic, solver);
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
					box._physicsposition = refer.Pose.Position;
					box._physicsrotation = Raymath.QuaternionToEuler(refer.Pose.Orientation) * (180 / MathF.PI);
					box._physicsvelocity = refer.Velocity.Linear;

					if (box._position.Y <= work.FallenPartsDestroyHeight)
					{
						box.Destroy();
						continue;
					}

					if (box.IsDirty)
						box.ReplicateProperties(["Position", "Rotation", "Velocity"], false);
				}
			}
		}
		public void ServerStep()
		{
			if (Workspace == null || DisablePhysics)
				return;

			var work = Workspace;

			LocalSimulation.Timestep(1.0f / 45, DefaultThreadDispatcher);

			for (int i = 0; i < Actors.Count; i++)
			{
				var box = Actors[i];

				if (!box.Anchored && box.Owner == null) // if part is dynamic AND its server-side
				{
					// reflect this in rendering
					Debug.Assert(box.BodyHandle.HasValue);

					var refer = LocalSimulation.Bodies[box.BodyHandle.Value];
					box._physicsposition = refer.Pose.Position;
					box._physicsrotation = Raymath.QuaternionToEuler(refer.Pose.Orientation) * (180 / MathF.PI);
					box._physicsvelocity = refer.Velocity.Linear;

					if (box._position.Y <= work.FallenPartsDestroyHeight)
					{
						box.Destroy();
						continue;
					}

					if (box.IsDirty)
						box.ReplicateProperties(["Position", "Rotation", "Velocity"], false);
				}
			}
		}
		public void Step()
		{
			try
			{
				if (GameManager.NetworkManager.IsServer)
					ServerStep();
				else if (GameManager.NetworkManager.IsClient)
					ClientStep();
			}
			catch (Exception e)
			{
				LogManager.LogError("Physics solver had failed! " + e.GetType() + ", msg:" + e.Message);
				LogManager.LogError(e.StackTrace ?? "no stacktrace");
				GameManager.Shutdown();
			}
		}
	}
	internal struct InternalNarrowPhaseCallbacks : INarrowPhaseCallbacks
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
		{
			return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
		{
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
		{
			pairMaterial.FrictionCoefficient = 2f;
			pairMaterial.MaximumRecoveryVelocity = 2f;
			pairMaterial.SpringSettings = new SpringSettings(30, 1);
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
		{
			return true;
		}
		public void Dispose()
		{
		}
		public void Initialize(Simulation simulation)
		{
		}
	}
	public struct InternalPoseIntegratorCallbacks : IPoseIntegratorCallbacks
	{
		public void Initialize(Simulation simulation)
		{
		}
		public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
		public bool AllowSubstepsForUnconstrainedBodies => false;
		public bool IntegrateVelocityForKinematics => false;
		public Vector3 Gravity;

		public InternalPoseIntegratorCallbacks(Vector3 gravity)
		{
			Gravity = gravity;
		}
		Vector3Wide gravityWideDt;

		public void PrepareForIntegration(float dt)
		{
			gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
		}
		public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
		{
			velocity.Linear += gravityWideDt;
		}
	}
}
