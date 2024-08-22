using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PhysicsTest2
{
	internal static class Program
	{
		internal static List<Box> AllBoxes = [];
		internal static Simulation Simulation;
		internal static BufferPool BufferPool;
		internal static Camera3D MainCamera;
		internal static bool Paused = false;

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
				pairMaterial.FrictionCoefficient = 1f;
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
		internal static void Main(string[] args)
		{
			MainCamera = new Camera3D(new Vector3(10, 10, 0), new Vector3(-1, 0, 0), Vector3.UnitY, 90, CameraProjection.Perspective);
			BufferPool = new();
			Simulation = Simulation.Create(BufferPool, new InternalNarrowPhaseCallbacks(), new InternalPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));

			Raylib.SetTraceLogLevel(TraceLogLevel.None);
			Raylib.InitWindow(1600, 900, "Physics Test 2");
			Raylib.SetTargetFPS(60);
			Raylib.SetExitKey(KeyboardKey.Null);

			Raylib.DisableCursor();

			AddBox(new Box()
			{
				Position = new Vector3(0, -5, 0),
				Size = new Vector3(2000, 3, 2000),
				Anchored = true
			});
			for (int i = 0; i < 100; i++)
			{
				AddBox(new Box()
				{
					Position = new Vector3(3, 10 + i * 3f, 0),
					Size = new Vector3(1, 2, 1),
					Anchored = false
				});
			}

			while (!Raylib.WindowShouldClose())
			{
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.SkyBlue);
				Raylib.BeginMode3D(MainCamera);

				if (Raylib.IsKeyPressed(KeyboardKey.Escape)) 
				{ 
					Paused = !Paused;
					if (Paused)
						Raylib.EnableCursor();
					else
						Raylib.DisableCursor();
				}
				if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
				{
					MainCamera.Position.Y -= 0.2f;
					MainCamera.Target.Y -= 0.2f;
				}
				if (Raylib.IsKeyDown(KeyboardKey.Space))
				{
					MainCamera.Position.Y += 0.2f;
					MainCamera.Target.Y += 0.2f;
				}

				if (!Paused)
					Raylib.UpdateCamera(ref MainCamera, CameraMode.FirstPerson);

				for (int i = 0; i < AllBoxes.Count; i++)
				{
					var box = AllBoxes[i];
					var dc = new Color((int)(box.Color.R * 0.8f), (int)(box.Color.G * 0.8f), (int)(box.Color.B * 0.8f), 255);
					Rlgl.PushMatrix();
					Rlgl.Translatef(box.Position.X, box.Position.Y, box.Position.Z);
					Rlgl.Rotatef(box.Rotation.X, 0.01f, 0, 0);
					Rlgl.Rotatef(box.Rotation.Y, 0, 0.01f, 0);
					Rlgl.Rotatef(box.Rotation.Z, 0, 0, 0.01f);

					Raylib.DrawCube(Vector3.Zero, box.Size.X, box.Size.Y, box.Size.Z, box.Color);
					Raylib.DrawCubeWires(Vector3.Zero, box.Size.X, box.Size.Y, box.Size.Z, dc);

					Rlgl.PopMatrix();

					if (box.Anchored)
					{
						for (float w = 0; w < box.Size.X; w++)
						{
							Raylib.DrawLine3D(
								new Vector3(box.Position.X - box.Size.X / 2 + w, box.Position.Y + box.Size.Y / 2 + 0.05f, box.Position.Z + box.Size.Z / 2),
								new Vector3(box.Position.X - box.Size.X / 2 + w, box.Position.Y + box.Size.Y / 2 + 0.05f, box.Position.Z - box.Size.Z / 2), dc);
						}
						for (float l = 0; l < box.Size.Z; l++)
						{
							Raylib.DrawLine3D(
								new Vector3(box.Position.X + box.Size.X / 2, box.Position.Y + box.Size.Y / 2 + 0.05f, box.Position.Z - box.Size.Z / 2 + l),
								new Vector3(box.Position.X - box.Size.X / 2, box.Position.Y + box.Size.Y / 2 + 0.05f, box.Position.Z - box.Size.Z / 2 + l), dc);
						}
					}
				}

				if (!Paused)
					Step();

				Raylib.EndMode3D();
				Raylib.DrawRectangle(800 - 2, 450 - 2, 4, 4, Paused ? Color.Red : Color.White);
				Raylib.EndDrawing();
			}

			Raylib.CloseWindow();
		}
		public static void AddBox(Box box)
		{
			if (box.Anchored)
			{
				box.StaticHandle = Simulation.Statics.Add(
					new StaticDescription(
						box.Position,
						Raymath.QuaternionFromEuler(box.Rotation.Z, box.Rotation.Y, box.Rotation.X),
						Simulation.Shapes.Add(
							new BepuPhysics.Collidables.Box(box.Size.X, box.Size.Y, box.Size.Z))));
			}
			else
			{
				var collidable = new BepuPhysics.Collidables.Box(box.Size.X, box.Size.Y, box.Size.Z);
				var inertia = collidable.ComputeInertia(1);
				box.BodyHandle = Simulation.Bodies.Add(
					BodyDescription.CreateDynamic(
						new RigidPose(
							box.Position,
							Raymath.QuaternionFromEuler(box.Rotation.Z, box.Rotation.Y, box.Rotation.X)),
						inertia,
						Simulation.Shapes.Add(collidable), 0.01f));
			}
			AllBoxes.Add(box);
		}
		public static void Step()
		{
			Simulation.Timestep(1 / 60.0f);
			for (int i = 0; i < AllBoxes.Count; i++)
			{
				var box = AllBoxes[i];
				if (!box.Anchored)
				{
					var refer = Simulation.Bodies[box.BodyHandle];
					box.Position = refer.Pose.Position;
					box.Rotation = Raymath.QuaternionToEuler(refer.Pose.Orientation) * (180 / MathF.PI);
					box.LinearVelocity = refer.Velocity.Linear;
					box.AngularVelocity = refer.Velocity.Angular;
				}
			}
		}
	}
	internal class Box
	{
		public Color Color = new Color(Random.Shared.Next(0, 255), Random.Shared.Next(0, 255), Random.Shared.Next(0, 255), 255);
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 LinearVelocity;
		public Vector3 AngularVelocity;
		public Vector3 Size;
		public bool Anchored;
		public StaticHandle StaticHandle;
		public BodyHandle BodyHandle;
	}
}
