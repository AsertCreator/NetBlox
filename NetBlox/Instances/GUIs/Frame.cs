using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class Frame : GuiObject
	{
		[Lua([Security.Capability.None])]
		public Color BackgroundColor3 { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 0;

		public Frame(GameManager ins) : base(ins) {	}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Frame) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				var s = Size.Calculate(cp, cs);
				Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor3.R, BackgroundColor3.G, BackgroundColor3.B, (int)((1 - BackgroundTransparency) * 255)));
			}
			base.RenderGUI(cp, cs);
		}
	}
}
