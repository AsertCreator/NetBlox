using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Hint : Message
	{
		public Hint(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Hint) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.RenderUI();
			GameManager.RenderManager.CurrentHint = Text;
		}
		public override void Destroy()
		{
			base.Destroy();
			GameManager.RenderManager.CurrentHint = null;
		}
	}
}
