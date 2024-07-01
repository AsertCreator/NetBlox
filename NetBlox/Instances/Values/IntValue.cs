using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class IntValue : Instance
	{
		[Lua([Security.Capability.None])]
		public long Value { get; set; }

		public IntValue(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(IntValue) == classname) return true;
			return base.IsA(classname);
		}
	}
}
