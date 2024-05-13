using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
	public class LuaSourceContainer : Instance
	{
		public LuaSourceContainer(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public string Source { get; set; } = string.Empty;

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(LuaSourceContainer) == classname) return true;
			return base.IsA(classname);
		}
	}
}
