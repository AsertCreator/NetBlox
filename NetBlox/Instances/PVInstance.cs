using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox.Instances
{
	public abstract class PVInstance : Instance
	{
		public PVInstance(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public abstract void PivotTo(CFrame pivot);
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PVInstance) == classname) return true;
			return base.IsA(classname);
		}
	}
}
