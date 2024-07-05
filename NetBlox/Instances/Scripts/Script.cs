using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
	[Creatable]
	public class Script : BaseScript
	{
		public Script(GameManager ins) : base(ins) { }

		public override void Process()
		{
			if (!HadExecuted && GameManager.NetworkManager.IsServer && Enabled && !GameManager.ProhibitScripts)
			{
				TaskScheduler.ScheduleScript(GameManager, Source, 2, this);
				HadExecuted = true;
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Script) == classname) return true;
			return base.IsA(classname);
		}
	}
}
