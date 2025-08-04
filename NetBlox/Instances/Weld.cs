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

				if (NormalizeOwnerships(Part0 as BasePart, Part1 as BasePart))
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
			if (NormalizeOwnerships(Part0 as BasePart, Part1 as BasePart))
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
			weldHandle = default;
		}
		/// <summary>
		/// Normalizes network ownerships between two parts for weld to work.
		/// </summary>
		/// <returns>true, if the weld can be simulated locally, otherwise false</returns>
		private bool NormalizeOwnerships(BasePart p0, BasePart p1)
		{
			if (p0 == null || p1 == null)
				return false;

			if (GameManager.NetworkManager.IsServer)
			{
				// p0 and p1 are both server-owned, we result in nothing
				if (p0.Owner == null && p1.Owner == null)
					return true;
				// p0 is client-owned and p1 is server-owned, we result in both being client-sided
				if (p0.Owner != null && p1.Owner == null)
				{
					p1.SetNetworkOwner(p0.Owner.Player);
					this.SetNetworkOwner(p0.Owner.Player);
					return false;
				}
				// p0 is server-owned and p1 is client-owned, we result in both being client-sided
				if (p0.Owner == null && p1.Owner != null)
				{
					p0.SetNetworkOwner(p1.Owner.Player);
					this.SetNetworkOwner(p1.Owner.Player);
					return false;
				}
				// p0 and p1 are both client-owned by different people, we result in both being server-sided
				if (p0.Owner != null && p1.Owner != null && p0.Owner != p1.Owner)
				{
					p0.SetNetworkOwner(null);
					p1.SetNetworkOwner(null);
					return true;
				}
				// p0 and p1 are both client-owned by the same person, we result in weld not being simulated here
				if (p0.Owner != null && p1.Owner != null && p0.Owner != p1.Owner)
				{
					this.SetNetworkOwner(p0.Owner.Player);
					return false;
				}
				return false;
			}
			else
			{
				if (p0.IsDomestic && p1.IsDomestic) // both are domestic
				{
					return true;
				}
				else
				{
					return false;
				}
			}
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

			PartOffset = b1.PartCFrame.Position - b0.PartCFrame.Position;

			Task.Run(async () => // god kill me
			{
				while (!b0.BodyHandle.HasValue || !b1.BodyHandle.HasValue)
					await Task.Yield();

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
