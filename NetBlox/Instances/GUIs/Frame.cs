using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class Frame : GuiObject
	{
		[Lua([Security.Capability.None])]
		public UDim2 Position { get; set; }
		[Lua([Security.Capability.None])]
		public UDim2 Size { get; set; }
		[Lua([Security.Capability.None])]
		public Color BackgroundColor { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 0;

		public Frame(GameManager ins) : base(ins) {	}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Frame) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			var p = Position.Calculate();
			var s = Size.Calculate();
			Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (int)((1 - BackgroundTransparency) * 255)));
			base.RenderUI();
		}
	}
}
