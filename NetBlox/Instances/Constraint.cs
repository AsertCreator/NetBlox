using NetBlox.Runtime;

namespace NetBlox.Instances
{
	public class Constraint : Instance
	{
		public Constraint(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Constraint) == classname) return true;
			return base.IsA(classname);
		}
	}
}
