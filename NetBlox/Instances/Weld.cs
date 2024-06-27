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
				if (value && !enabled)
				{
					b0.Actor.Body.AddBox(b1.Actor.BoxDef);
					b1.Actor.Body.RemoveBox(b1.Actor.Box);
				}
				if (!value && enabled)
				{
					b1.Actor.Body.AddBox(b1.Actor.BoxDef);
					b0.Actor.Body.RemoveBox(b1.Actor.Box);
				}
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
