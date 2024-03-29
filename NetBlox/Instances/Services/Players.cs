using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class Players : Instance
	{
		[Lua]
		public Instance? LocalPlayer { get; set; }

		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Players) == classname) return true;
			return base.IsA(classname);
		}
	}
}
