using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
    public class ReplicatedStorage : Instance
	{
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(ReplicatedStorage) == classname) return true;
			return base.IsA(classname);
		}
	}
}
