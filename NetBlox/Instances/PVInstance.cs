using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances
{
	public class PVInstance : Instance
	{
		protected CFrame Origin;
		protected CFrame PivotOffset;

		public PVInstance(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public CFrame GetPivot()
		{
			return PivotOffset + Origin;
		}
		[Lua([Security.Capability.None])]
		public void SetPivot(CFrame pivot)
		{
			Origin = pivot;
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PVInstance) == classname) return true;
			return base.IsA(classname);
		}
	}
}
