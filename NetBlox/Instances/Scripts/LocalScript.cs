using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
	[Creatable]
	public class LocalScript : BaseScript
	{
		public LocalScript(GameManager ins) : base(ins) { }

		public override void Process()
		{
			if (!HadExecuted && GameManager.NetworkManager.IsClient && Enabled && !GameManager.ProhibitScripts) // we can only execute 
			{
				TaskScheduler.ScheduleScript(GameManager, Source, 2, this);
				HadExecuted = true;
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(LocalScript) == classname) return true;
			return base.IsA(classname);
		}
	}
}
