using Raylib_cs;
using NetBlox.Structs;

namespace NetBlox.GUI
{
	public class GUIButton : GUIElement
	{
		public Color Color = Color.Gray;
		public string Text = "Text";
		public int FontSize = 20;
		public Action? OnClick;

		public GUIButton(string text, UDim2 pos) { Text = text; Position = pos; }

		public override string GetUIType() => nameof(GUIText);
		public override void Render(int sx, int sy)
		{
			if (Visible)
			{
				int width = (int)Raylib.MeasureTextEx(RenderManager.MainFont, Text, FontSize, 0).X;
				int ax = (int)(sx * Position.X + Position.XOff - width / 2);
				int ay = (int)(sy * Position.Y + Position.YOff - FontSize / 2);
				int bsx = (int)(sx * Size.X + Position.XOff);
				int bsy = (int)(sy * Size.Y + Position.YOff);

				Raylib.DrawTextEx(RenderManager.MainFont, Text, new(ax, ay), FontSize, 0, Color);

				int mx = Raylib.GetMouseX();
				int my = Raylib.GetMouseY();

				Raylib.DrawRectangleLines(ax - bsx / 2, ay - bsy / 2, bsx, bsy, Color);

				if (mx <= (ax + bsx / 2) && my <= (ay + bsy / 2) &&
					mx >= (ax - bsx / 2) && my >= (ay - bsy / 2) &&
					Raylib.IsMouseButtonDown(MouseButton.Left))
				{
					if (OnClick != null)
						OnClick();
				}
			}
		}
	}
}
