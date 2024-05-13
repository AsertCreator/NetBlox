using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class Backpack : Instance
	{
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Backpack) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Reload()
		{
			var sg = GameManager.CurrentRoot.GetService("StarterPack");
			var ss = sg.GetChildren();

			ClearAllChildren();

			for (int i = 0; i < ss.Length; i++)
			{
				var child = ss[i];
				var clone = child.Clone();
				clone.Parent = this;
			}
		}
	}
}
