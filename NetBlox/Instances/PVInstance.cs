using NetBlox.Runtime;
using NetBlox.Structs;

namespace NetBlox.Instances
{
	public class PVInstance : Instance
	{
		protected CFrame Origin;
		protected CFrame PivotOffset;

		[Lua]
		public CFrame GetPivot()
		{
			return PivotOffset + Origin;
		}
		[Lua]
		public void SetPivot(CFrame pivot)
		{
			Origin = pivot;
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(PVInstance) == classname) return true;
			return base.IsA(classname);
		}
	}
}
