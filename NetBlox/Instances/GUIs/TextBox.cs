using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class TextBox : GuiObject
	{
		[Lua([Security.Capability.None])]
		public string Text { get; set; } = "";
		[Lua([Security.Capability.None])]
		public Color BackgroundColor3 { get; set; } = Color.White;
		[Lua([Security.Capability.None])]
		public Color TextColor3 { get; set; } = Color.Black;
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 1;
		[Lua([Security.Capability.None])]
		public float FontSize { get; set; } = 16;
		[Lua([Security.Capability.None])]
		public bool LeftAligned { get; set; } = false;

		public TextBox(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(TextBox) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				var s = Size.Calculate(Vector2.Zero, cs);
				Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Color(BackgroundColor3.R, BackgroundColor3.G, BackgroundColor3.B, (int)((1 - BackgroundTransparency) * 255)));
				if (!LeftAligned)
				{
					var m = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Text, FontSize, FontSize / 10);
					Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Text, p + s / 2 - m / 2, FontSize, 0, TextColor3);
				}
				else
					Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Text, p + new Vector2(0, s.Y / 2 - FontSize / 2), FontSize, 0, TextColor3);
				
				if (GameManager.RenderManager.FocusedBox == this)
				{
					if (Raylib.IsKeyPressed(KeyboardKey.Escape))
						GameManager.RenderManager.FocusedBox = null;
					if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
						Text = Text[..^1];

					char a = (char)Raylib.GetCharPressed();
					while (a != 0 && GameManager.RenderManager.FocusedBox == this)
					{
						Text += a;
						a = (char)Raylib.GetKeyPressed();
					}
				}
			}
			base.RenderGUI(cp, cs);
		}
		public override void Activate(MouseButton mb)
		{
			if (mb == MouseButton.Left)
				GameManager.RenderManager.FocusedBox = this;
		}
	}
}
