using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class ObjectValue : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Value { get; set; }

		public ObjectValue(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ObjectValue) == classname) return true;
			return base.IsA(classname);
		}
	}
}
