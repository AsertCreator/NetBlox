using MoonSharp.Interpreter;
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
			TaskScheduler.CurrentJob.JoinedUntil = wa;
			return DynValue.NewYieldReq([]); // here we go to the next, bc thread is paused
		}
		[MoonSharpModuleMethod]
		public static DynValue spawn(ScriptExecutionContext x, CallbackArguments y)
		{
			Job cjob = TaskScheduler.CurrentJob;
			Job job = TaskScheduler.ScheduleScript(cjob.GameManager, y[0], (int)(cjob.AssociatedObject1 ?? 1), cjob.AssociatedObject2 as BaseScript);
			return DynValue.NewCoroutine(job.AssociatedObject0 as Coroutine);
		}
		[MoonSharpModuleMethod]
		public static DynValue delay(ScriptExecutionContext x, CallbackArguments y)
		{
			Job cjob = TaskScheduler.CurrentJob;
			Job job = TaskScheduler.ScheduleScript(cjob.GameManager, y[1], (int)(cjob.AssociatedObject1 ?? 1), cjob.AssociatedObject2 as BaseScript);
			job.JoinedUntil = DateTime.UtcNow.AddSeconds(y[0].Number);
			return DynValue.NewCoroutine(job.AssociatedObject0 as Coroutine);
		}
		[MoonSharpModuleMethod]
		public static DynValue cancel(ScriptExecutionContext x, CallbackArguments y)
		{
			var job = TaskScheduler.RunningJobs.Find(j => j.AssociatedObject0 == y[0]);
			if (job == null)
				throw new Exception("No such job is running!");
			TaskScheduler.Terminate(job);
			if (job == TaskScheduler.CurrentJob)
				return DynValue.NewYieldReq([]);
			return DynValue.Void;
		}
		[MoonSharpModuleMethod]
		public static DynValue defer(ScriptExecutionContext x, CallbackArguments y) => spawn(x, y); // events still run defered anyway
	}
}
