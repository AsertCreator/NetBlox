using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	public class GuiObject : Instance
	{
		[Lua([Security.Capability.None])]
		public int ZIndex { get; set; } = 1;
		[Lua([Security.Capability.None])]
		public UDim2 Position { get; set; }
		[Lua([Security.Capability.None])]
		public UDim2 Size { get; set; }
		[Lua([Security.Capability.None])]
		public bool Visible { get; set; } = true;

		public GuiObject(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(GuiObject) == classname) return true;
			return base.IsA(classname);
		}
		public virtual void RenderGUI(Vector2 cp, Vector2 cs)
		{
			if (!Visible) return;
			var tor = from x in Children where x is GuiObject orderby -((GuiObject)x).ZIndex select x;
			var c = tor.Count();
			for (int i = 0; i < c; i++)
			{
				GuiObject go = (GuiObject)tor.ElementAt(i);
				go.RenderGUI(Position.Calculate(cp, cs), Size.Calculate(Vector2.Zero, cs));
			}
		}
		public virtual GuiObject? HitTest(Vector2 cp, Vector2 cs)
		{
			if (!Visible) return null;
			var tor = from x in Children where x is GuiObject orderby ((GuiObject)x).ZIndex select x;
			var esz = Size.Calculate(Vector2.Zero, cs);
			var epo = Position.Calculate(cp, cs);
			var c = tor.Count();
			for (int i = 0; i < c; i++)
			{
				var el = tor.ElementAt(i) as GuiObject;
				if (el == null) continue;
				var sz = el.Size.Calculate(Vector2.Zero, esz);
				var po = el.Position.Calculate(epo, esz);
				if (RenderUtils.MouseCollides(po, sz))
				{
					var t = el.HitTest(epo, esz);
					if (t != null) return t;
				}
			}
			return this;
		}
		public virtual void Activate(MouseButton mb)
		{
			// do nothing
		}
	}
}
