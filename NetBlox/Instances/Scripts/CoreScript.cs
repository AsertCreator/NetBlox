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
			if (!HadExecuted && Enabled && !GameManager.ProhibitScripts) // we can only execute 
			{
				TaskScheduler.ScheduleScript(GameManager, Source, 3, this);
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
