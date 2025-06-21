using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
    public class BillboardGui : Instance
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;
		[Lua([Security.Capability.None])]
		public UDim2 Size
		{
			get => size;
			set
			{
				size = value;
				sizeDirty = true;
			}
		}
		[Lua([Security.Capability.None])]
		public BasePart? Adornee { get; set; }

		private UDim2 size;
		private int onScreenOffsetX = 0;
		private int onScreenOffsetY = 0;
		private bool sizeDirty = true;
		private RenderTexture2D? renderTexture;

		public BillboardGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ScreenGui) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			if (Enabled && Adornee != null)
			{
				var tor = from x in Children where x is GuiObject orderby ((GuiObject)x).ZIndex select x;
				var ssz = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
				var sz = Size.Calculate(default, ssz);
				var c = tor.Count();

				if (sizeDirty)
				{
					if (!renderTexture.HasValue)
						Raylib.UnloadRenderTexture(renderTexture.Value);
					renderTexture = Raylib.LoadRenderTexture((int)sz.X, (int)sz.Y);
				}

				Raylib.BeginTextureMode(renderTexture.Value);

				for (int i = 0; i < c; i++)
				{
					GuiObject go = (GuiObject)tor.ElementAt(i);
					go.RenderGUI(Vector2.Zero, ssz);

					var act = go.HitTest(Vector2.Zero, ssz, Raylib.GetMouseX() - onScreenOffsetX, Raylib.GetMouseY() - onScreenOffsetY);

					if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Left))
						act.Activate(MouseButton.Left);
					else if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Right))
						act.Activate(MouseButton.Right);
					else if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Middle))
						act.Activate(MouseButton.Middle);
				}

				Raylib.EndTextureMode();

				var point = Raylib.GetWorldToScreen(Adornee._position, GameManager.RenderManager.MainCamera);
				onScreenOffsetX = (int)point.X;
				onScreenOffsetY = (int)point.Y;

				Raylib.DrawTexture(renderTexture.Value.Texture, onScreenOffsetX, onScreenOffsetY, Color.White);
			}
		}
	}
}
