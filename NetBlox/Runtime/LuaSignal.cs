using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;

namespace NetBlox.Runtime
{
	public class LuaSignal
	{
		public List<LuaConnection> Attached = new();
		public List<Action<DynValue[]>> NativeAttached = new();
		public int FireCount = 0;

		[Lua([Security.Capability.None])]
		public void Connect(DynValue dv)
		{
			if (TaskScheduler.CurrentJob == null)
				return;
			lock (this) 
				Attached.Add(new LuaConnection()
				{
					Function = dv,
					Level = (int)TaskScheduler.CurrentJob.AssociatedObject1,
					Manager = ((BaseScript)TaskScheduler.CurrentJob.AssociatedObject2).GameManager,
					Script = (BaseScript)TaskScheduler.CurrentJob.AssociatedObject2
				});
		}
		[Lua([Security.Capability.None])]
		public void Wait()
		{
			int c = FireCount;
			while (c == FireCount) ;
		}
		public void Fire(params DynValue[] dvs)
		{
			lock (this)
			{
				for (int i = 0; i < Attached.Count; i++)
					TaskScheduler.ScheduleScript(Attached[i].Manager, Attached[i].Function, Attached[i].Level, Attached[i].Script)
						.AssociatedObject4 = dvs;
				for (int i = 0; i < NativeAttached.Count; i++)
					NativeAttached[i](dvs);
				FireCount++;
			}
		}
	}
	public class LuaConnection
	{
		public GameManager? Manager;
		public BaseScript? Script;
		public DynValue? Function;
		public int Level;
	}
}
