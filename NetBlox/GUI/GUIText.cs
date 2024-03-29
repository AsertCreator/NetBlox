using Raylib_cs;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.GUI
{
	public class GUIText : GUIElement
	{
		public Color Color = Color.White;
		public string Text = "Text";
		public int FontSize = 16;

		public GUIText(string text, UDim2 pos) { Text = text; Position = pos; }
		public override string GetUIType() => nameof(GUIText);
		public override void Render(int sx, int sy)
		{
			if (Visible)
			{
				var siz = Raylib.MeasureTextEx(RenderManager.MainFont, Text, FontSize, 0);

				Raylib.DrawTextEx(RenderManager.MainFont, Text,
					new Vector2(
						(int)(sx * Position.X + Position.XOff - siz.X / 2),
						(int)(sy * Position.Y + Position.YOff - siz.Y / 2)), 
					FontSize, 0, Color);
			}
		}
	}
}
