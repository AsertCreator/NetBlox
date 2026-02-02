using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	[Service]
	public class ReplicatedStorage : Instance
	{
		public ReplicatedStorage(GameManager ins) : base(ins)
		{
			GameManager.RegisterService(this, ServiceType.ReplicatedStorage);
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ReplicatedStorage) == classname) return true;
			return base.IsA(classname);
		}
	}
}
