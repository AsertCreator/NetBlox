using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class ImageLabel : GuiObject
	{
		[Lua([Security.Capability.None])]
		public string FilePath
		{
			get => fp;
			set
			{
				fp = value;
				LoadImage(fp);
			}
		}
		[Lua([Security.Capability.None])]
		public float BackgroundTransparency { get; set; } = 1;
		private string fp = "";
		private Color[,] colors = new Color[0, 0];
		private int width = 0;
		private int height = 0;

		public ImageLabel(GameManager ins) : base(ins) { }
		public void LoadImage(string fp)
		{
			fp = AppManager.ResolveUrl(fp);
			using var img = SixLabors.ImageSharp.Image.Load(fp).CloneAs<Rgba32>();
			colors = new Color[img.Width, img.Height];
			width = img.Width;
			height = img.Height;
			for (int y = 0; y < img.Height; y++)
				for (int x = 0; x < img.Width; x++)
					colors[x, y] = new(img[x, y].R, img[x, y].G, img[x, y].B, img[x, y].A);
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ImageLabel) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (Visible)
			{
				var p = Position.Calculate(cp, cs);
				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						var c = colors[x, y];
						c.A = (byte)(BackgroundTransparency * 255);
						Raylib.DrawPixel((int)(x + p.X), (int)(y + p.Y), c);
					}
			}
			base.RenderGUI(cp, cs);
		}
	}
}
