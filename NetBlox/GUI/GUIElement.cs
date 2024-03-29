using NetBlox.Structs;

namespace NetBlox.GUI
{
	public abstract class GUIElement
	{
		public UDim2 Size;
		public UDim2 Position;
		public bool Visible = true;

		public abstract string GetUIType();
		public abstract void Render(int sx, int sy);
	}
}
