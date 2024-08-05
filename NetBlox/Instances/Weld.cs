using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Weld : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Part0 { get; set; }
		[Lua([Security.Capability.None])]
		public Instance? Part1 { get; set; }
		[Lua([Security.Capability.None])]
		public bool Enabled 
		{
			get => enabled;
			set
			{
				BasePart? b0 = Part0 as BasePart;
				BasePart? b1 = Part1 as BasePart;
				if (b0 == null || b1 == null)
				{
					LogManager.LogWarn("Part0 and Part1 properties of Weld only support BaseParts!");
					return;
				}
				if (b0.Body != null && b1.Body != null && b0.Box != null && b1.Box != null)
				{
					if (value && !enabled)
					{
						b0.Body.AddBox(b1.BoxDef);
						b1.Body.RemoveBox(b1.Box);
					}
					if (!value && enabled)
					{
						b1.Body.AddBox(b1.BoxDef);
						b0.Body.RemoveBox(b1.Box);
					}
				}
				else
					LogManager.LogWarn("Part0 and Part1 properties of Weld are only supported on owned parts!");
				enabled = value;
			}
		}
		private bool enabled;

		public Weld(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Weld) == classname) return true;
			return base.IsA(classname);
		}
	}
}
