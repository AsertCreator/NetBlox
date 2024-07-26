using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class Chat : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Chatted { get; init; } = new();

		public Chat(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Chat) == classname) return true;
			return base.IsA(classname);
		}
	}
}
