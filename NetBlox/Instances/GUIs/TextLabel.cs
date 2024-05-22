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
	public class TextLabel : GuiObject
	{
		[Lua([Security.Capability.None])]
		public string Text { get; set; } = "";
		[Lua([Security.Capability.None])]
		public Color BackgroundColor { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public Color ForegroundColor { get; set; } = Color.Black;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 1;
		[Lua([Security.Capability.None])]
		public float FontSize { get; set; } = 16;

		public TextLabel(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(TextLabel) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				var s = Size.Calculate(cp, cs);
				var m = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Text, FontSize, FontSize / 10);
				Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (int)((1 - BackgroundTransparency) * 255)));
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Text, p + s / 2 - m / 2, FontSize, 0, ForegroundColor);
			}
			base.RenderGUI(cp, cs);
		}
	}
}
