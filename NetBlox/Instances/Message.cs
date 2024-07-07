using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Message : Instance
	{
		[Lua([Security.Capability.None])]
		public string Text { get; set; }

		public Message(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Message) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.RenderUI();
			GameManager.RenderManager.CurrentMessage = Text;
		}
		public override void Destroy()
		{
			base.Destroy();
			GameManager.RenderManager.CurrentMessage = null;
		}
	}
}
