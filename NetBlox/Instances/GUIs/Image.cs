using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class Image : GuiObject
	{
		[Lua([Security.Capability.None])]
		public UDim2 Position { get; set; }
		[Lua([Security.Capability.None])]
		public UDim2 Size { get; set; }
		[Lua([Security.Capability.None])]
		public string FilePath { get; set; } = "rbxasset://textures/somethingidontlike.png"; // i tried to load that but i(t) just died
		private Texture2D handle;

		public Image(GameManager ins) : base(ins)
		{
			var fp = AppManager.ResolveUrl(FilePath);
			handle = GameManager.RenderManager.StudTexture; // so instead i will use this
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Image) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			if (Visible)
			{
				var p = Position.Calculate();
				Raylib.DrawTexture(handle, (int)p.X, (int)p.Y, Color.White);
			}
			base.RenderUI();
		}
	}
}
