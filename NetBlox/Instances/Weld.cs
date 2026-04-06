using Jitter2.Dynamics.Constraints;
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

				if (joint != null)
				{
					if (value)
						joint.Enable();
					else
						joint.Disable();
				}

				enabled = value;
			}
		}

		private WeldJoint? joint;
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

			base.DestroyConstraint();

			var sim = GameManager.PhysicsManager.LocalSimulation;
			if (joint != null)
				joint.Remove();
			joint = null;
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

			if (!part0.IsDomestic || !part1.IsDomestic)
				return;

			if (joint != null) 
			{
				LogManager.LogWarn("Trying to create a weld joint in place of an already existing one!"); 
			}

			joint = new WeldJoint(GameManager.PhysicsManager.LocalSimulation, part0.CurrentRigidBody, part1.CurrentRigidBody, part0.Position);

			base.CreateConstraint();
		}
	}
}
