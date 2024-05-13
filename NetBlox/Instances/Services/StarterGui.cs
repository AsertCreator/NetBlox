using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class StarterGui : Instance
	{
		public StarterGui(GameManager ins) : base(ins) { }
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(StarterGui) == classname) return true;
			return base.IsA(classname);
		}
	}
}
