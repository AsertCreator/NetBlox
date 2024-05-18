using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Scripts
{
	public class CoreScript : BaseScript
	{
		public CoreScript(GameManager ins) : base(ins) { }

		public override void Process()
		{
			if (!HadExecuted && GameManager.NetworkManager.IsClient && Enabled) // we can only execute 
			{
				LuaRuntime.Execute(Source, 3, GameManager, this);
				HadExecuted = true;
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(CoreScript) == classname) return true;
			return base.IsA(classname);
		}
	}
}
