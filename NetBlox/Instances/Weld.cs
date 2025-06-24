using BepuPhysics;
using BepuPhysics.Constraints;
using NetBlox.Runtime;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Weld : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Part0
		{
			get => part0;
			set
			{
				if (part0 == value) return;
				var enabled = Enabled;
				Enabled = false;
				part0 = value;
				Enabled = enabled;
			}
		}
		[Lua([Security.Capability.None])]
		public Instance? Part1
		{
			get => part1;
			set
			{
				if (part1 == value) return;
				var enabled = Enabled;
				Enabled = false;
				part1 = value;
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

				if (IsDomestic)
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
		private Instance? part0;
		private Instance? part1;
		private bool enabled;

		public Weld(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Weld) == classname) return true;
			return base.IsA(classname);
		}
		public override void OnNetworkOwnershipChanged()
		{
			if (IsDomestic)
			{
				if (Enabled)
					CreateWeld();
				else
					DestroyWeld();
			}
		}
		public override void Destroy()
		{
			if (Enabled)
				DestroyWeld();
			base.Destroy();
		}
		private void DestroyWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;

			sim.Solver.Remove(weldHandle);
		}
		private void CreateWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;
			var b0 = Part0 as BasePart;
			var b1 = Part1 as BasePart;

			if (Part0 == Part1)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld cannot be set to the same part!");
				return;
			}

			if (b0 == null || b1 == null)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld only support BaseParts!");
				return;
			}

			PartOffset = b1.PartCFrame.Position - b0.PartCFrame.Position;

			Task.Run(async () => // god kill me
			{
				while (!b0.BodyHandle.HasValue || !b1.BodyHandle.HasValue)
					await Task.Delay(1);

				weld = new BepuPhysics.Constraints.Weld()
				{
					LocalOffset = PartOffset,
					LocalOrientation = Quaternion.Identity,
					SpringSettings = new SpringSettings(30, 0.1f)
				};

				TaskScheduler.Schedule(() =>
				{
					weldHandle = sim.Solver.Add(b0.BodyHandle.Value, b1.BodyHandle.Value, weld);
				});
			});
		}
	}
}
