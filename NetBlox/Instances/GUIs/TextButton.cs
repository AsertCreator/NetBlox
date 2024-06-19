using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_CsLo;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class TextButton : GuiObject
	{
		[Lua([Security.Capability.None])]
		public string Text { get; set; } = "";
		[Lua([Security.Capability.None])]
		public LuaSignal MouseButton1Click { get; init; } = new();
		[Lua([Security.Capability.None])]
		public Color BackgroundColor3 { get; set; } = Raylib.WHITE;
		[Lua([Security.Capability.None])]
		public Color TextColor3 { get; set; } = Raylib.BLACK;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 1;
		[Lua([Security.Capability.None])]
		public float FontSize { get; set; } = 16;

		public TextButton(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(TextButton) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				var s = Size.Calculate(Vector2.Zero, cs);
				var m = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Text, FontSize, FontSize / 10);
				Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor3.r, BackgroundColor3.g, BackgroundColor3.b, (int)((1 - BackgroundTransparency) * 255)));
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Text, p + s / 2 - m / 2, FontSize, 0, TextColor3);
			}
			base.RenderGUI(cp, cs);
		}
		public override void Activate(MouseButton mb)
		{
			if (mb == MouseButton.MOUSE_BUTTON_LEFT)
				MouseButton1Click.Fire();
		}
	}
}
