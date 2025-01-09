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
		public Vector2 AbsolutePosition => absolutePosition;
		[Lua([Security.Capability.None])]
		public Vector2 AbsoluteSize => absoluteSize;
		[Lua([Security.Capability.None])]
		public Vector2 AnchorPoint { get; set; }
		[Lua([Security.Capability.None])]
		public bool Visible { get; set; } = true;
		[Lua([Security.Capability.None])]
		public LuaSignal MouseEnter { get; set; } = new();
		[Lua([Security.Capability.None])]
		public LuaSignal MouseLeave { get; set; } = new();

		private Vector2 absolutePosition;
		private Vector2 absoluteSize;

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
			var count = tor.Count();

			var esz = Size.Calculate(Vector2.Zero, cs);
			var epo = Position.Calculate(cp, cs);

			absolutePosition = epo - (esz * AnchorPoint);
			absoluteSize = esz;

			for (int i = 0; i < count; i++)
			{
				GuiObject go = (GuiObject)tor.ElementAt(i);
				go.RenderGUI(epo, esz);
			}
		}
		public virtual GuiObject? HitTest(Vector2 cp, Vector2 cs)
		{
			if (!Visible) return null;

			var tor = from x in Children where x is GuiObject orderby ((GuiObject)x).ZIndex select x;
			var count = tor.Count();

			var esz = Size.Calculate(Vector2.Zero, cs);
			var epo = Position.Calculate(cp, cs);

			absolutePosition = epo - (esz * AnchorPoint);
			absoluteSize = esz;

			if (RenderUtils.MouseCollides(epo, esz))
			{
				for (int i = 0; i < count; i++)
				{
					GuiObject go = (GuiObject)tor.ElementAt(i);
					var res = go.HitTest(epo, esz);
					if (res != null)
						return res;
				}

				return this;
			}

			return null!;
		}
		public virtual void Activate(MouseButton mb)
		{
			// do nothing
		}
	}
}
