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
		public override BasePart? Part0
		{
			get => part0;
			set
			{
				if (part0 == value) return;

				if (part0 != null)
				{
					part0.BeforePhysicsRepresentationChanged -= PhysicsRepresentationChangedHandler;
					part0.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				}

				if (Enabled)
					DestroyConstraint();

				part0 = value;
				if (part0 != null)
				{
					part0.BeforePhysicsRepresentationChanged += PhysicsRepresentationChangedHandler;
					part0.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
				}

				if (Enabled && part0 != null)
					CreateConstraint();
			}
		}
		[Lua([Security.Capability.None])]
		public override BasePart? Part1
		{
			get => part1;
			set
			{
				if (part1 == value) return;

				if (part1 != null)
				{
					part1.BeforePhysicsRepresentationChanged -= PhysicsRepresentationChangedHandler;
					part1.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				}

				if (Enabled)
					DestroyConstraint();

				part1 = value;
				if (part1 != null)
				{
					part1.BeforePhysicsRepresentationChanged += PhysicsRepresentationChangedHandler;
					part1.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
				}

				if (Enabled && part1 != null)
					CreateConstraint();
			}
		}
		[Lua([Security.Capability.None])]
		public override Vector3 PartOffset { get; set; }
		[Lua([Security.Capability.None])]
		public override bool Enabled 
		{
			get => enabled;
			set
			{
				if (value == enabled)
					return;

				if (part0 == null || part1 == null)
				{
					DestroyConstraint();
					return;
				}

				if (part0.IsDomestic && part1.IsDomestic)
				{
					if (value)
						CreateConstraint();
					else
						DestroyConstraint();
				}

				enabled = value;
			}
		}

		private BepuPhysics.Constraints.Weld weld;
		private ConstraintHandle weldHandle;
		private BasePart? part0;
		private BasePart? part1;
		private bool enabled;

		public Weld(GameManager ins) : base(ins) { }

		private void PhysicsRepresentationChangedHandler(object sender, EventArgs args)
		{
			ReevaluateConstraint();
		}
		private void NetworkOwnershipChangedHandler(object sender, EventArgs args)
		{
			ReevaluateConstraint();
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
				DestroyConstraint();
			base.Destroy();
		}
		public override void ReevaluateConstraint()
		{
			if (Enabled)
			{
				DestroyConstraint();
				CreateConstraint();
			}
		}
		public override void DestroyConstraint()
		{
			if (!Enabled)
				return;

			GameManager.PhysicsManager.DeferredPhysicsActions.Enqueue(() =>
			{
				base.DestroyConstraint();

				var sim = GameManager.PhysicsManager.LocalSimulation;
				if (sim.Solver.ConstraintExists(weldHandle))
				{
					sim.Solver.Remove(weldHandle);
				}
			});
		}
		public override void CreateConstraint()
		{
			if (!Enabled)
				return;

			var sim = GameManager.PhysicsManager.LocalSimulation;

			if (part0 == null || part1 == null)
				return;

			if (part0 == part1)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld cannot be set to the same part!");
				return;
			}

			PartOffset = part1.PartCFrame.Position - part0.PartCFrame.Position;

			GameManager.PhysicsManager.DeferredPhysicsActions.Enqueue(() =>
			{
				if (!part0.IsDomestic || !part1.IsDomestic)
					return;

				if (!part0.BodyHandle.HasValue || !part1.BodyHandle.HasValue)
				{
					if (part0.BodyHandle.HasValue && !part1.BodyHandle.HasValue)
						part0.AnchoredFactorWeldToAnchored = true;
					if (!part0.BodyHandle.HasValue && part1.BodyHandle.HasValue)
						part1.AnchoredFactorWeldToAnchored = true;
					if (!part0.BodyHandle.HasValue && !part1.BodyHandle.HasValue)
						return;
					return;
				}

				weld = new BepuPhysics.Constraints.Weld()
				{
					LocalOffset = PartOffset,
					LocalOrientation = Quaternion.Identity,
					SpringSettings = new SpringSettings(30, 0.1f)
				};

				weldHandle = sim.Solver.Add(part0.BodyHandle.Value, part1.BodyHandle.Value, weld);
				base.CreateConstraint();
			});
		}
	}
}
