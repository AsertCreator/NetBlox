using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class ReplicatedFirst : Instance
	{
		public ReplicatedFirst(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public void RemoveDefaultLoadingScreen()
		{
			// umm do something
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ReplicatedFirst) == classname) return true;
			return base.IsA(classname);
		}
	}
}
