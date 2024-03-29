using NetBlox.Runtime;
using NetBlox.Instances;

namespace NetBlox.Instances.Services
{
    public class Workspace : Instance
    {
        [Lua]
		[Replicated]
		public Instance? MainCamera { get; set; }

		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Workspace) == classname) return true;
			return base.IsA(classname);
		}
	}
}
