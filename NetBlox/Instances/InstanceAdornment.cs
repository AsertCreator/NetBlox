using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class InstanceAdornment : Instance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public BasePart? Adornee { get; set; }

		public InstanceAdornment(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(InstanceAdornment) == classname) return true;
			return base.IsA(classname);
		}
		public virtual void Decorate(BasePart part) { }
		public void Render()
		{
			if (Adornee != null)
				Decorate(Adornee);
		}
	}
}
