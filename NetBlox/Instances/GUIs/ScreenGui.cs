using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class ScreenGui : GuiObject
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;

		public ScreenGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ScreenGui) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			if (Enabled)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].RenderUI();
				}
			}
		}
	}
}
