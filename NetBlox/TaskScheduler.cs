using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public class Job
	{
		public JobType Type = JobType.NativeMisc;
		public object? AssociatedObject0;
		public object? AssociatedObject1;
		public object? AssociatedObject2;
		public object? AssociatedObject3;
		public object? AssociatedObject4;
		/// <summary>
		/// When <seealso cref="Type"/> is set to <seealso cref="JobType.Script"/>, then its set to script's returned value
		/// </summary>
		public object? AssociatedObject5;
		public GameManager GameManager;
		public JobDelegate NativeCallback;
		public JobDelegate? AfterDone;
		public DateTime JoinedUntil = DateTime.MinValue;
		public bool HadRunBefore = false;
		public JobResult Result;
		public Job? JoinedTo;
	}
	public enum JobType { NativeNetwork, NativeRender, NativePhysics, NativeMisc, Script }
	public enum JobResult
	{
		CompletedSuccess, CompletedFailure, NotCompleted
	}
	public enum PressureType
	{
		None, Slightly, Mildly, Severe, Unplayable, Fatal
	}
	public delegate JobResult JobDelegate(Job self);
	public static class TaskScheduler
	{
		public static bool AllowScheduling = true;
		public static TimeSpan LastCycleTime = TimeSpan.Zero;
		public static int JobCount => RunningJobs.Count;
		public static double AverageTimeToRun => LastCycleTime.TotalMilliseconds / RunningJobs.Count;
		public static PressureType PressureType
		{
			get
			{
				if (JobCount <= 3) return PressureType.None;
				if (JobCount <= 13) return PressureType.Slightly;
				if (JobCount <= 23) return PressureType.Mildly;
				if (JobCount <= 33) return PressureType.Severe;
				if (JobCount <= 43) return PressureType.Unplayable;
				return PressureType.Fatal;
			}
		}
		public static Job CurrentJob;
		private static List<Job> RunningJobs = [];

		public static void Step()
		{
			Stopwatch sw = new();
			sw.Start();
			if (RunningJobs.Count != 0)
			{
				var now = DateTime.Now;

				for (int i = 0; i < RunningJobs.Count; i++)
				{
					var job = RunningJobs[i];
					if (job.JoinedUntil > now)
						continue;
					if (job.JoinedTo != null && job.JoinedTo.Result == JobResult.NotCompleted)
						continue;
					CurrentJob = job;
					try
					{
						var res = job.NativeCallback(job);
						job.HadRunBefore = true;
						job.Result = res;
						if (res != JobResult.NotCompleted)
						{
							job.AfterDone?.Invoke(job);
							Terminate(job);
							i--;
						}
					}
					catch (Exception ex)
					{
						LogManager.LogError("Job execution error:" + ex.Message + "; the job will be terminated");
						Terminate(job);
						i--;
					}
				}
			}
			sw.Stop();

			if (RunningJobs.Count >= 43)
				LogManager.LogWarn("Task Scheduler: cannot keep up at all!!!, too many jobs (" + RunningJobs.Count + ")");
			else if (RunningJobs.Count >= 36)
				LogManager.LogWarn("Task Scheduler: cannot keep up, too many jobs (" + RunningJobs.Count + ")");

			LastCycleTime = sw.Elapsed;
		}
		public static void Terminate(Job job) => RunningJobs.Remove(job);
		public static void ScheduleMisc(JobDelegate jd, JobDelegate? afterDone = null) => RunningJobs.Add(new()
		{
			NativeCallback = jd,
			AfterDone = afterDone,
			Type = JobType.NativeMisc
		});
		public static void ScheduleNetwork(GameManager gm, JobDelegate jd, JobDelegate? afterDone = null) => RunningJobs.Add(new()
		{
			NativeCallback = jd,
			AfterDone = afterDone,
			Type = JobType.NativeNetwork,
			GameManager = gm
		});
		public static void ScheduleRender(JobDelegate jd, JobDelegate? afterDone = null) => RunningJobs.Add(new()
		{
			NativeCallback = jd,
			AfterDone = afterDone,
			Type = JobType.NativeRender
		});
		public static void SchedulePhysics(GameManager gm, JobDelegate jd, JobDelegate? afterDone = null) => RunningJobs.Add(new()
		{
			NativeCallback = jd,
			AfterDone = afterDone,
			Type = JobType.NativePhysics,
			GameManager = gm
		});
		public static Job ScheduleScript(GameManager gm, string code, int level, BaseScript? self, JobDelegate? afterDone = null, DynValue[]? args = null)
		{
			try
			{
				return ScheduleScript(gm, gm.MainEnvironment.LoadString(code, null, self != null ? self.GetFullName() : ""), level, self, afterDone, args);
			}
			catch (SyntaxErrorException ex)
			{
				LogManager.LogError("Syntax error: " + ex.Message);
				return null!;
			}
		}
		public static Job ScheduleScript(GameManager gm, DynValue func, int level, BaseScript? self, JobDelegate? afterDone = null, DynValue[]? args = null)
		{
			var closure = gm.MainEnvironment.CreateCoroutine(func).Coroutine;
			var job = new Job()
			{
				NativeCallback = delegate (Job job)
				{
					try
					{
						Workspace? works = gm.CurrentRoot.GetService<Workspace>(true);

						if (works == null)
							gm.MainEnvironment.Globals["workspace"] = DynValue.Nil;
						else
							gm.MainEnvironment.Globals["workspace"] = LuaRuntime.MakeInstanceTable(works, gm);

						if (self == null)
							gm.MainEnvironment.Globals["script"] = DynValue.Nil;
						else
							gm.MainEnvironment.Globals["script"] = LuaRuntime.MakeInstanceTable(self, gm);

						var args = job.AssociatedObject4 as DynValue[];
						var result = closure.Resume(args);

						if (closure.State == CoroutineState.Suspended)
							return JobResult.NotCompleted;
						else
							job.AssociatedObject5 = result;
						return JobResult.CompletedSuccess;
					}
					catch (ScriptRuntimeException ex)
					{
						LogManager.LogError("Script error: " + ex.Message);
						for (int i = 0; i < ex.CallStack.Count; i++)
							LogManager.LogError($"    at {ex.CallStack[i].Name ?? ""}:{((ex.CallStack[i].Location != null) ? ex.CallStack[i].Location.FromLine.ToString() : "(unknown)")}");
						return JobResult.CompletedFailure;
					}
					catch (Exception ex)
					{
						LogManager.LogError("Script execution error:" + ex.Message + "; the job will be terminated");
						return JobResult.CompletedFailure;
					}
				},
				AfterDone = afterDone,
				AssociatedObject0 = closure,
				AssociatedObject1 = level,
				AssociatedObject2 = self,
				AssociatedObject4 = args ?? [],
				Type = JobType.Script,
				GameManager = gm
			};
			RunningJobs.Add(job);
			return job;
		}
	}
}
