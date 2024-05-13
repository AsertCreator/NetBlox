using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Scripts
{
	public class CoreScript : BaseScript
	{
		public override void Process()
		{
			if (!HadExecuted && NetworkManager.IsClient && Enabled) // we can only execute 
			{
				LuaRuntime.Execute(Source, 3, this, GameManager.CurrentRoot);
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
