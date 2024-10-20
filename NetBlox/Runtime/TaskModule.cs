using MoonSharp.Interpreter;
using MoonSharp.Interpreter.DataTypes;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Scripts;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	/// <summary>
	/// Represents a 'task' library, available in Lua. Its implemented in that way, bc im too lazy.
	/// </summary>
	[MoonSharpModule(Namespace = "task")]
	public class TaskModule
	{
		[MoonSharpModuleMethod]
		public static DynValue wait(ScriptExecutionContext x, CallbackArguments y)
		{ // i just copied the original wait lol
			var wa = y.Count == 0 ? DateTime.UtcNow : DateTime.UtcNow.AddSeconds(y[0].Number);
			TaskScheduler.CurrentJob.JobTimingContext.JoinedUntil = wa;
			return DynValue.NewYieldReq([]); // here we go to the next, bc thread is paused
		}
		[MoonSharpModuleMethod]
		public static DynValue spawn(ScriptExecutionContext x, CallbackArguments y)
		{
			Job cjob = TaskScheduler.CurrentJob;
			Job job = TaskScheduler.ScheduleScript(cjob.ScriptJobContext.GameManager, y[0], cjob.SecurityLevel, cjob.ScriptJobContext.BaseScript);
			return DynValue.NewCoroutine(job.ScriptJobContext.Coroutine);
		}
		[MoonSharpModuleMethod]
		public static DynValue delay(ScriptExecutionContext x, CallbackArguments y)
		{
			Job cjob = TaskScheduler.CurrentJob;
			Job job = TaskScheduler.ScheduleScript(cjob.ScriptJobContext.GameManager, y[1], cjob.SecurityLevel, cjob.ScriptJobContext.BaseScript);
			job.JobTimingContext.JoinedUntil = DateTime.UtcNow.AddSeconds(y[0].Number);
			return DynValue.NewCoroutine(job.ScriptJobContext.Coroutine);
		}
		[MoonSharpModuleMethod]
		public static DynValue cancel(ScriptExecutionContext x, CallbackArguments y)
		{
			var job = TaskScheduler.RunningJobs.Find(j => j.ScriptJobContext.Coroutine == y[0].Coroutine) ?? throw new ScriptRuntimeException("No such job is running!");
			TaskScheduler.Terminate(job);
			if (job == TaskScheduler.CurrentJob)
				return DynValue.NewYieldReq([]);
			return DynValue.Void;
		}
		[MoonSharpModuleMethod]
		public static DynValue defer(ScriptExecutionContext x, CallbackArguments y) => spawn(x, y); // events still run defered anyway
	}
}
