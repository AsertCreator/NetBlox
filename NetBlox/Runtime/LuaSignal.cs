using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;
using System.Diagnostics;
using System.Xml.Linq;

namespace NetBlox.Runtime
{
	public class LuaSignal
	{
		public GameManager GameManager;
		public List<LuaConnection> Attached = [];
		public List<Action<DynValue[]>> NativeAttached = [];
		public ulong FireCount = 0;

		public LuaSignal(GameManager gameManager)
		{
			GameManager = gameManager;
		}

		[Lua([Security.Capability.None])]
		public void Connect(DynValue dv)
		{
			if (TaskScheduler.CurrentJob == null)
				return;
			lock (this) 
			{
				if (TaskScheduler.CurrentJob.ScriptJobContext.BaseScript == null)
					return;

				Attached.Add(new LuaConnection()
				{
					Function = dv,
					Level = TaskScheduler.CurrentJob.SecurityLevel,
					Manager = TaskScheduler.CurrentJob.ScriptJobContext.BaseScript.GameManager,
					Script = TaskScheduler.CurrentJob.ScriptJobContext.BaseScript
				});
			}
		}
		[Lua([Security.Capability.None])]
		public LuaYield Wait()
		{
			var c = FireCount;
			var job = TaskScheduler.CurrentJob;

			job.JobTimingContext.TaskJoinedTo = Task.Run(async() =>
			{
				while (!GameManager.ShuttingDown)
				{
					if (c == FireCount)
						await Task.Yield();
					else
					{
						job.ScriptJobContext.YieldReturn = [DynValue.Void];
						return;
					}
				}
			});

			return new();
		}
		public void Fire(params DynValue[] dvs)
		{
			lock (this)
			{
				for (int i = 0; i < Attached.Count; i++)
				{
					if (Attached[i].Manager == null) continue;
					if (Attached[i].Function == null) continue;

#pragma warning disable CS8604 // Possible null reference argument.
					TaskScheduler.ScheduleScript(Attached[i].Manager, Attached[i].Function, Attached[i].Level, Attached[i].Script)
						.ScriptJobContext.YieldReturn = dvs;
#pragma warning restore CS8604 // Possible null reference argument.
				}
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
