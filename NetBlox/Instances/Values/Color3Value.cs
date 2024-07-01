using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Values
{
	[Creatable]
	public class Color3Value : Instance
	{
		[Lua([Security.Capability.None])]
		public Color Value { get; set; }

		public Color3Value(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Color3Value) == classname) return true;
			return base.IsA(classname);
		}
	}
}
