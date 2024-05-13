using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class TextLabel : GuiObject
	{
		[Lua([Security.Capability.None])]
		public UDim2 Position { get; set; }
		[Lua([Security.Capability.None])]
		public UDim2 Size { get; set; }
		[Lua([Security.Capability.None])]
		public string Text { get; set; } = "";
		[Lua([Security.Capability.None])]
		public Color BackgroundColor { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public Color ForegroundColor { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 1;
		[Lua([Security.Capability.None])]
		public float FontSize { get; set; } = 16;

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(TextLabel) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			var p = Position.Calculate();
			var s = Size.Calculate();
			var m = Raylib.MeasureTextEx(RenderManager.MainFont, Text, FontSize, FontSize / 10);
			Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (int)((1 - BackgroundTransparency) * 255)));
			Raylib.DrawTextEx(RenderManager.MainFont, Text, p + s / 2 - m / 2, FontSize, 0, ForegroundColor);
			base.RenderUI();
		}
	}
}
