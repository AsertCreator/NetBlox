using Raylib_cs;
using NetBlox.Structs;

namespace NetBlox.GUI
{
	public class GUIFrame : GUIElement
	{
		public Color Color = Color.Gray;

		public GUIFrame(UDim2 sz, UDim2 pos, Color back) { Position = pos; Size = sz; Color = back; }
		public override string GetUIType() => nameof(GUIText);
		public override void Render(int sx, int sy)
		{
			if (Visible)
			{
				int szx = (int)(sx * Size.X + Size.XOff);
				int szy = (int)(sy * Size.Y + Size.YOff);
				int posx = (int)(sx * Position.X + Position.XOff) - szx / 2;
				int posy = (int)(sy * Position.Y + Position.YOff) - szy / 2;

				Raylib.DrawRectangle(posx, posy, szx, szy, Color);
			}
		}
	}
}
