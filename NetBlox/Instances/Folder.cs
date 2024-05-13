using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Folder : Instance
	{
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Folder) == classname) return true;
			return base.IsA(classname);
		}
	}
}
