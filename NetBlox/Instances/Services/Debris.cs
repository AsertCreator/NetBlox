using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class Debris : Instance
	{
		public Debris(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Debris) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void AddItem(Instance ins, double when) => ins.DestroyAt = DateTime.UtcNow.AddSeconds(when);
	}
}
