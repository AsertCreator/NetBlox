using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public class ServerStorage : Instance
	{
		public ServerStorage(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ServerStorage) == classname) return true;
			return base.IsA(classname);
		}
	}
}
