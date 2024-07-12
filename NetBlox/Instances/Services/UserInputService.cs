using MoonSharp.Interpreter;
using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class UserInputService : Instance
	{
		public UserInputService(GameManager ins) : base(ins) { }
		[Lua([Security.Capability.None])]
		// TOCUH I CANT
		public bool TouchEnabled => GameManager.CurrentProfile.IsTouchDevice;
		[Lua([Security.Capability.CoreSecurity])]
		public LuaSignal KeyboardPress { get; init; } = new();

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(UserInputService) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
			int kc = Raylib.GetKeyPressed();
			while (kc != 0)
			{
				KeyboardPress.Fire(DynValue.NewNumber(kc));
				kc = Raylib.GetKeyPressed();
			}
		}
	}
}
