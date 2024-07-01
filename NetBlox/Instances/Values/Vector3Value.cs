using NetBlox.Runtime;
using System.Numerics;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class Vector3Value : Instance
	{
		[Lua([Security.Capability.None])]
		public Vector3 Value { get; set; }

		public Vector3Value(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Vector3Value) == classname) return true;
			return base.IsA(classname);
		}
	}
}
