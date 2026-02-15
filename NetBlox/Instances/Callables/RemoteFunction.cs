using MoonSharp.Interpreter;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances.Callables
{
	[Creatable]
	public class RemoteFunction : Instance
	{
		public RemoteFunction(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public DynValue Invoke()
		{
			LogManager.LogError("TODO: implement RemoteFunctions");
			return DynValue.Void;
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RemoteFunction) == classname) return true;
			return base.IsA(classname);
		}
	}
}
