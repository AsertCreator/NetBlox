using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	[Creatable]
	public class ScreenGui : Instance
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;

		public ScreenGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ScreenGui) == classname) return true;
			return base.IsA(classname);
		}
		public override void RenderUI()
		{
			if (Enabled)
			{
				var tor = from x in Children where x is GuiObject orderby ((GuiObject)x).ZIndex select x;
				var ssz = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
				var c = tor.Count();

				for (int i = 0; i < c; i++)
				{
					GuiObject go = (GuiObject)tor.ElementAt(i);
					go.RenderGUI(Vector2.Zero, ssz);

					var act = go.HitTest(Vector2.Zero, ssz, Raylib.GetMouseX(), Raylib.GetMouseY());

					if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Left))
						act.Activate(MouseButton.Left);
					else if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Right))
						act.Activate(MouseButton.Right);
					else if (act != null && Raylib.IsMouseButtonPressed(MouseButton.Middle))
						act.Activate(MouseButton.Middle);
				}
			}
		}
	}
}
