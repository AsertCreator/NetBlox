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
					part0.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				part0 = value;
				if (part0 != null)
					part0.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
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
					part1.OnNetworkOwnershipChanged -= NetworkOwnershipChangedHandler;
				part1 = value;
				if (part1 != null)
					part1.OnNetworkOwnershipChanged += NetworkOwnershipChangedHandler;
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

		public Weld(GameManager ins) : base(ins) { }

		private void NetworkOwnershipChangedHandler(object sender, EventArgs args)
		{
			Enabled = !Enabled;
			Enabled = !Enabled;
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
		private void DestroyWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;
			if (sim.Solver.ConstraintExists(weldHandle)) 
			{
				sim.Solver.Remove(weldHandle); 
			}
		}
		private void CreateWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;

			if (part0 == part1)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld cannot be set to the same part!");
				return;
			}

			PartOffset = part1.PartCFrame.Position - part0.PartCFrame.Position;

			Task.Run(async () => // god kill me
			{
				while (!part0.BodyHandle.HasValue || !part1.BodyHandle.HasValue)
					await Task.Yield();

				weld = new BepuPhysics.Constraints.Weld()
				{
					LocalOffset = PartOffset,
					LocalOrientation = Quaternion.Identity,
					SpringSettings = new SpringSettings(30, 0.1f)
				};

				TaskScheduler.Schedule(() =>
				{
					weldHandle = sim.Solver.Add(part0.BodyHandle.Value, part1.BodyHandle.Value, weld);
				});
			});
		}
	}
}
