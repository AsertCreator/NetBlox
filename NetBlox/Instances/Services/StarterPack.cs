using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class StarterPack : Instance
	{
		public StarterPack(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(StarterPack) == classname) return true;
			return base.IsA(classname);
		}
	}
}
