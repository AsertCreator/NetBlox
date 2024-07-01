using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class NumberValue : Instance
	{
		[Lua([Security.Capability.None])]
		public double Value { get; set; }

		public NumberValue(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(NumberValue) == classname) return true;
			return base.IsA(classname);
		}
	}
}
