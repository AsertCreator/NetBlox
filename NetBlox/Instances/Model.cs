using NetBlox.Runtime;

namespace NetBlox.Instances
{
	[Creatable]
	public class Model : PVInstance
	{
		public Model(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Model) == classname) return true;
			return base.IsA(classname);
		}
	}
}
