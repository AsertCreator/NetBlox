using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class ImageButton : GuiObject
	{
		[Lua([Security.Capability.None])]
		public string Image 
		{ 
			get => fp;
			set 
			{
				fp = value;
				if (GameManager.RenderManager != null)
					RenderManager.LoadTexture(fp, x => texture = x);
			} 
		}
		[Lua([Security.Capability.None])]
		public LuaSignal MouseButton1Click { get; init; } = new();
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 0;
		private Texture2D? texture;
		private string fp = "";

		public ImageButton(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ImageButton) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				var s = Size.Calculate(Vector2.Zero, cs);
				if (texture.HasValue)
					Raylib.DrawTexture(texture.Value, (int)p.X, (int)p.Y, new Raylib_cs.Color(255, 255, 255, (int)((1 - BackgroundTransparency) * 255f)));
			}
			base.RenderGUI(cp, cs);
		}
		public override void Activate(MouseButton mb)
		{
			if (mb == MouseButton.Left)
				MouseButton1Click.Fire();
		}
	}
}
