using Jitter2;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Network;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox
{
	public class PhysicsManager
	{
		public GameManager GameManager;
		public Workspace? Workspace => GameManager.CurrentRoot.GetService<Workspace>(true);
		public float Gravity
		{
			get => LocalSimulation.Gravity.Y;
			set => LocalSimulation.Gravity = 
				new Jitter2.LinearMath.JVector(LocalSimulation.Gravity.X, value, LocalSimulation.Gravity.Z);
		}
		public World LocalSimulation;
		public bool IsLowPoweredDevice = false;

		public List<BasePart> Actors = new();
		public List<Humanoid> Humanoids = new();

		public Queue<Action> DeferredPhysicsActions = [];

		public bool DisablePhysics = true; // not now
		internal Stopwatch physicsStopwatch = new();

		public PhysicsManager(GameManager gameManager)
		{
			GameManager = gameManager;

			LocalSimulation = new World();
			if (!IsLowPoweredDevice)
				LocalSimulation.SubstepCount = 4;
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

			LocalSimulation.Step(1.0f / AppManager.PreferredPhysicsRate);

			for (int i = 0; i < Actors.Count; i++)
			{
				var box = Actors[i];

				if (!box.IsActuallyAnchored && box.IsDomestic) // if part is dynamic AND its domestic
				{
					// reflect this in rendering
					var refer = box.CurrentRigidBody;

					if (box.IsDescendantOf(work))
					{
						box._physicsposition = refer.Position;
						box._physicsrotation = refer.Orientation;
						box._physicsvelocity = refer.Velocity;
						box.RotationalVelocity = refer.AngularVelocity;

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

			LocalSimulation.Step(1.0f / AppManager.PreferredPhysicsRate);

			var clients = GameManager.NetworkManager.Clients;

			for (int i = 0; i < Actors.Count; i++)
			{
				var box = Actors[i];

				if (!box.IsActuallyAnchored && box.IsDomestic) // if part is dynamic AND its server-side
				{
					// reflect this in rendering

					var refer = box.CurrentRigidBody;

					if (box.IsDescendantOf(work))
					{
						box._physicsposition = refer.Position;
						box._physicsrotation = refer.Orientation;
						box._physicsvelocity = refer.Velocity;
						box.RotationalVelocity = refer.AngularVelocity;

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
						refer.SetActivationState(false);
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

			try
			{
				if (DeferredPhysicsActions.Any())
					DeferredPhysicsActions.Dequeue()();
			}
			catch (Exception e)
			{
				LogManager.LogError("DeferredPhysicsActions had failed! " + e.GetType() + ", msg:" + e.Message);
				LogManager.LogError(e.StackTrace ?? "no stacktrace");
			}
		}
		public float QuickRaycast(Vector3 from, Vector3 direction, float max)
		{
			return 322887;
		}
	}
}
