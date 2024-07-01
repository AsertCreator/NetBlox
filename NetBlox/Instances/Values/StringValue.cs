using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class StringValue : Instance
	{
		[Lua([Security.Capability.None])]
		public string Value { get; set; }

		public StringValue(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(StringValue) == classname) return true;
			return base.IsA(classname);
		}
	}
}
