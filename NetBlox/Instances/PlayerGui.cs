using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class PlayerGui : Instance
	{
		public PlayerGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PlayerGui) == classname) return true;
			return base.IsA(classname);
		}
	}
}
