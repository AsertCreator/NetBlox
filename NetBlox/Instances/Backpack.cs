using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class Backpack : Instance
	{
		public Backpack(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Backpack) == classname) return true;
			return base.IsA(classname);
		}
	}
}
