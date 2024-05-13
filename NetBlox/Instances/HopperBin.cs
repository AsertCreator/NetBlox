using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class HopperBin : Tool
	{
		[Lua([Security.Capability.None])]
		public int BinType { get; set; }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(HopperBin) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public override void Activate() 
		{ 
			
		}
		[Lua([Security.Capability.CoreSecurity])]
		public override void SetSelected()
		{
			
		}
		[Lua([Security.Capability.CoreSecurity])]
		public override void SetUnselected()
		{

		}
	}
}
