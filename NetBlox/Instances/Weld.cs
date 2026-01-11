using BepuPhysics;
using BepuPhysics.Constraints;
using NetBlox.Runtime;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Weld : Constraint
	{
		[Lua([Security.Capability.None])]
		public BasePart? Part0
		{
			get => part0;
			set
			{
				if (part0 == value) return;

				var enabled = Enabled;

				Enabled = false;

				if (part0 != null)
				{
					part0.BeforePhysicsRepresentationChanged -= PhysicsRepresentationChangedHandler;
					part0.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				}

				part0 = value;
				if (part0 != null)
				{
					part0.BeforePhysicsRepresentationChanged += PhysicsRepresentationChangedHandler;
					part0.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
				}

				Enabled = enabled;
			}
		}
		[Lua([Security.Capability.None])]
		public BasePart? Part1
		{
			get => part1;
			set
			{
				if (part1 == value) return;

				var enabled = Enabled;

				Enabled = false;

				if (part1 != null)
				{
					part1.BeforePhysicsRepresentationChanged -= PhysicsRepresentationChangedHandler;
					part1.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				}

				part1 = value;
				if (part1 != null)
				{
					part1.BeforePhysicsRepresentationChanged += PhysicsRepresentationChangedHandler;
					part1.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
				}

				Enabled = enabled;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 PartOffset { get; set; }
		[Lua([Security.Capability.None])]
		public bool Enabled 
		{
			get => enabled;
			set
			{
				if (value == enabled)
					return;

				if (part0 == null || part1 == null)
				{
					DestroyWeld();
					return;
				}

				if (part0.IsDomestic && part1.IsDomestic)
				{
					if (value)
						CreateWeld();
					else
						DestroyWeld();
				}

				enabled = value;
			}
		}

		private BepuPhysics.Constraints.Weld weld;
		private ConstraintHandle weldHandle;
		private BasePart? part0;
		private BasePart? part1;
		private bool enabled;
		private Job? waitingJob;

		public Weld(GameManager ins) : base(ins) { }

		private void PhysicsRepresentationChangedHandler(object sender, EventArgs args)
		{
			Reevaluate();
		}
		private void NetworkOwnershipChangedHandler(object sender, EventArgs args)
		{
			Reevaluate();
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Weld) == classname) return true;
			return base.IsA(classname);
		}
		public override void Destroy()
		{
			if (Enabled)
				DestroyWeld();
			base.Destroy();
		}
		private void Reevaluate()
		{
			if (Enabled)
			{
				DestroyWeld();
				CreateWeld();
			}
		}
		private void DestroyWeld()
		{
			if (waitingJob != null)
				TaskScheduler.Terminate(waitingJob);

			var sim = GameManager.PhysicsManager.LocalSimulation;
			if (sim.Solver.ConstraintExists(weldHandle)) 
			{
				sim.Solver.Remove(weldHandle); 
			}
		}
		private void CreateWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;

			if (part0 == null || part1 == null)
				return;

			if (part0 == part1)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld cannot be set to the same part!");
				return;
			}

			PartOffset = part1.PartCFrame.Position - part0.PartCFrame.Position;

			if (!part0.IsDomestic || !part1.IsDomestic)
				return;

			weld = new BepuPhysics.Constraints.Weld()
			{
				LocalOffset = PartOffset,
				LocalOrientation = Quaternion.Identity,
				SpringSettings = new SpringSettings(30, 0.1f)
			};

			if (waitingJob != null)
				TaskScheduler.Terminate(waitingJob);

			waitingJob = TaskScheduler.ScheduleNamedJob("WeldWaiting", JobType.Miscellaneous, _ =>
			{
				BasePart? originalPart0 = Part0;
				BasePart? originalPart1 = Part1;

				if (originalPart0 == null || originalPart1 == null)
					return JobResult.CompletedFailure;

				while (originalPart0 == part0 && originalPart1 == part1 &&
					(!originalPart0.BodyHandle.HasValue || !originalPart1.BodyHandle.HasValue))
				{
					return JobResult.NotCompleted;
				}

				weldHandle = sim.Solver.Add(part0.BodyHandle.Value, part1.BodyHandle.Value, weld);
				return JobResult.CompletedSuccess;
			});
		}
	}
}
