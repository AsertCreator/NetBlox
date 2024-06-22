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
		private Task<string>? fsa;
		[Lua([Security.Capability.None])]
		public LuaYield<string> FilterStringAsync(string text, Instance from, Instance to)
		{
			if (fsa != null)
			{
				if (fsa.IsCompleted)
				{
					var res = new LuaYield<string>()
					{
						HasResult = true,
						Result = fsa.Result,
					};
					fsa = null;
					return res;
				}
				return new() { HasResult = false };
			}
			fsa = Task.Run(() => {
				if (GameManager.NetworkManager!.IsServer)
					return GameManager.FilterString(text);
				return "not filtering lol"; // i cant believe that the actual implementation is here
			});
			return new() { HasResult = false };
		}
	}
}
