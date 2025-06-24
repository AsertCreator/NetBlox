using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	public class Backpack : Instance
	{
		[Lua([Security.Capability.None])]
		public Tool? Selected => sel;
		private Tool? sel;

		public Backpack(GameManager ins) : base(ins)
		{
			if (GameManager.NetworkManager.IsClient && false) // nah
			{
				Hopper hopper = new(ins);
				hopper.Parent = this;
				hopper.BinType = HopperType.Drag;
				sel = hopper;
			}
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Backpack) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void Select(Tool tool)
		{
			Unselect();
			sel = tool;
			tool.SetSelected();
		}
		[Lua([Security.Capability.None])]
		public void Unselect() => sel?.SetUnselected();
		public override void RenderUI()
		{
			if (Raylib.IsMouseButtonPressed(MouseButton.Left))
				sel?.Activate();
		}
	}
}
