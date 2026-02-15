using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances
{
	public class BasePlayerGui : Instance
	{
		public BasePlayerGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(BasePlayerGui) == classname) return true;
			return base.IsA(classname);
		}
	}
}
