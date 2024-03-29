using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class ReplicatedFirst : Instance
	{
		[Lua]
		public void RemoveDefaultLoadingScreen()
		{
			// umm do something
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(ReplicatedFirst) == classname) return true;
			return base.IsA(classname);
		}
	}
}
