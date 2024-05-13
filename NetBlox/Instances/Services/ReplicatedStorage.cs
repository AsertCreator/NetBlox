using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class ReplicatedStorage : Instance
	{
		public ReplicatedStorage(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ReplicatedStorage) == classname) return true;
			return base.IsA(classname);
		}
	}
}
