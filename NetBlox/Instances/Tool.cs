using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Tool : Instance
	{
		public Tool(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Tool) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public virtual string GetIcon()
		{
			return "rbxasset://textures/stud.png";
		}
		[Lua([Security.Capability.CoreSecurity])]
		public virtual void Activate() { }
		[Lua([Security.Capability.CoreSecurity])]
		public virtual void SetSelected() { }
		[Lua([Security.Capability.CoreSecurity])]
		public virtual void SetUnselected() { }
	}
}
