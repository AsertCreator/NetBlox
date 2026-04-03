using NetBlox.Runtime;
using System.Numerics;

namespace NetBlox.Instances
{
	public class Constraint : Instance
	{
		[Lua([Security.Capability.None])]
		public virtual BasePart? Part0
		{
			get => null;
			set { }
		}
		[Lua([Security.Capability.None])]
		public virtual BasePart? Part1
		{
			get => null;
			set { }
		}
		[Lua([Security.Capability.None])]
		public virtual Vector3 PartOffset { get; set; }
		[Lua([Security.Capability.None])]
		public virtual bool Enabled
		{
			get => false;
			set { }
		}

		public Constraint(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Constraint) == classname) return true;
			return base.IsA(classname);
		}
		public virtual void ReevaluateConstraint()
		{
			if (Enabled)
			{
				DestroyConstraint();
				CreateConstraint();
			}
		}
		public virtual void DestroyConstraint() { }
		public virtual void CreateConstraint() { }
	}
}
