using MoonSharp.Interpreter;
using MoonSharp.Interpreter.DataTypes;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Diagnostics;

namespace NetBlox
{
	public delegate JobResult JobDelegate(Job self);
	public struct ScriptJobContext
	{
		public Table GlobalEnv;
		public Coroutine? Coroutine;
		public BaseScript? BaseScript;
		public GameManager GameManager;
		public DynValue[] YieldReturn;
		public DynValue YieldAnswer;
		public JobDelegate? AfterDone;
	}
	public struct JobTimingContext
	{
		public DateTime JoinedUntil;
		public bool HadRunBefore;
		public double LastCycleTime;
		public Task? TaskJoinedTo;
		public Job? JoinedTo;
		public int Priority;

		public DateTime LastExecutionTime;
		public DateTime LastLastExecutionTime;

		public DateTime LastTotalExecutionTime;
		public DateTime LastLastTotalExecutionTime;
	}
	public class Job(JobType type, JobDelegate callback, int security)
	{
		public JobType Type = type;
		public string Name = "Unknown job - look in the JIT debugger";
		public int SecurityLevel = security;
		public JobDelegate NativeCallback = callback;
		public ScriptJobContext ScriptJobContext = new();
		public JobTimingContext JobTimingContext = new();
		public JobResult Result = JobResult.NotCompleted;

		public override string ToString() =>
			(ScriptJobContext.GameManager != null ? ScriptJobContext.GameManager.ManagerName : "<none>") + 
			"-" + Type + ",level=" + SecurityLevel;
	}
	public enum JobType { Network, Renderer, Heartbeat, Miscellaneous, Physics, Script }
	public enum JobResult { CompletedSuccess, CompletedFailure, NotCompleted }

	// i actually can't write proper task schedulers, can i?
	public static class TaskScheduler
	{
		public static Job CurrentJob;
		public static bool Enabled = true;
		public static TimeSpan LastCycleTime = TimeSpan.Zero;
		public static int JobCount => RunningJobs.Count;
		public static double AverageTimeToRun => LastCycleTime.TotalMilliseconds / RunningJobs.Count;
		internal static List<Job> RunningJobs = [];
		internal static int DefaultPriority = 1;

		public static void Step()
		{
			Stopwatch sw = new();
			sw.Start();

			// we're so thread-safe

			Job[] currentJobPool;

			lock (RunningJobs) 
			{
				currentJobPool = new Job[RunningJobs.Count];
				RunningJobs.CopyTo(currentJobPool, 0);
			}

			for (int i = 0; i < currentJobPool.Length; i++)
			{
				var job = currentJobPool[i];
				var now = DateTime.UtcNow;

				if (job == null)
				{
					RunningJobs.RemoveAt(i);
					// just skip it
					continue;
				}

				if (job.JobTimingContext.JoinedUntil > now)
					continue;
				if (job.JobTimingContext.JoinedTo != null && job.JobTimingContext.JoinedTo.Result == JobResult.NotCompleted)
					continue;
				if (job.JobTimingContext.TaskJoinedTo != null && !job.JobTimingContext.TaskJoinedTo.IsCompleted)
					continue;

				CurrentJob = job;

				job.JobTimingContext.JoinedTo = null;
				job.JobTimingContext.JoinedUntil = default;

				job.JobTimingContext.LastLastTotalExecutionTime = job.JobTimingContext.LastTotalExecutionTime;
				job.JobTimingContext.LastTotalExecutionTime = now;

				try
				{
					JobResult res = JobResult.CompletedSuccess;
					Stopwatch taswa = new();

					var letnow = DateTime.UtcNow;

					for (int j = 0; j < job.JobTimingContext.Priority; j++)
					{
						taswa.Reset();
						taswa.Start();

						job.JobTimingContext.LastLastExecutionTime = job.JobTimingContext.LastExecutionTime;
						job.JobTimingContext.LastExecutionTime = letnow;

						res = job.NativeCallback(job);
						job.JobTimingContext.HadRunBefore = true;
						job.Result = res;

						if (res != JobResult.NotCompleted)
						{
							job.ScriptJobContext.AfterDone?.Invoke(job);
							Terminate(job);
							break;
						}

						taswa.Stop();

						job.JobTimingContext.LastCycleTime = taswa.Elapsed.TotalSeconds;
					}

					if (res != JobResult.NotCompleted)
						break;
				}
				catch (Exception ex)
				{
					LogManager.LogError("Job execution error:" + ex.Message + "; the job will be terminated");
					Terminate(job);
				}
			}

			sw.Stop();

			LastCycleTime = sw.Elapsed;
		}
		public static void Terminate(Job job) => RunningJobs.Remove(job);
		public static Job ScheduleNamedJob(string name, JobType type, JobDelegate jd, JobDelegate? afterDone = null, int level = 8)
		{
			Job job = new(type, jd, level);
			job.Name = name;
			job.ScriptJobContext.AfterDone = afterDone;
			job.JobTimingContext.Priority = DefaultPriority;
			lock (RunningJobs)
				RunningJobs.Add(job);
			return job;
		}
		public static Job ScheduleDelayedNamedJob(string name, TimeSpan delay, JobType type, JobDelegate jd, JobDelegate? afterDone = null, int level = 8)
		{
			Job job = new(type, jd, level);
			job.Name = name;
			job.JobTimingContext.JoinedUntil = DateTime.UtcNow + delay;
			job.ScriptJobContext.AfterDone = afterDone;
			job.JobTimingContext.Priority = DefaultPriority;
			lock (RunningJobs)
				RunningJobs.Add(job);
			return job;
		}
		public static Job ScheduleScript(GameManager gm, string code, int level, BaseScript? self, JobDelegate? afterDone = null, DynValue[]? args = null)
		{
			try
			{
				return ScheduleScript(gm, gm.MainEnvironment.LoadString(code, new Table(gm.MainEnvironment)
				{
					IsProtected = true,
					ObjectType = AssociatedObjectType.Misc,

					["script"] = LuaRuntime.PushInstance(self),
					["workspace"] = LuaRuntime.PushInstance(gm.CurrentRoot.GetService<Workspace>(true)),
					["Workspace"] = LuaRuntime.PushInstance(gm.CurrentRoot.GetService<Workspace>(true)),

					MetaTable = new Table(gm.MainEnvironment)
					{
						["__index"] = gm.MainEnvironment.Globals
					}
				}, self != null ? self.GetFullName() : ""), level, self, afterDone, args);
			}
			catch (SyntaxErrorException ex)
			{
				LogManager.LogError("Syntax error: " + ex.Message);
				return null!;
			}
		}
		public static Job ScheduleScript(GameManager gm, DynValue func, int level, BaseScript? self, JobDelegate? afterDone = null, DynValue[]? args = null)
		{
			if ((level == 7 || level == 8) && gm.NetworkManager.IsClient)
				throw new Exception("Server-exclusive threads are not expected on client!");

			Coroutine? closure = null;
			if (func.Type == DataType.Function) closure = gm.MainEnvironment.CreateCoroutine(func).Coroutine;
			else if (func.Type == DataType.Thread) closure = func.Coroutine;
			else throw new InvalidOperationException("Cannot create a thread with not a function or coroutine");

			var job = new Job(JobType.Script, ScriptJob, level);
			job.Name = "Script-" + func.ToDebugPrintString();
			job.ScriptJobContext.GameManager = gm;
			job.ScriptJobContext.AfterDone = afterDone;
			job.ScriptJobContext.BaseScript = self;
			job.ScriptJobContext.YieldReturn = args ?? [];
			job.ScriptJobContext.Coroutine = closure;
			job.JobTimingContext.Priority = DefaultPriority;

			lock (RunningJobs)
				RunningJobs.Add(job);
			return job;
		}
		private static JobResult ScriptJob(Job job)
		{
			if (job.ScriptJobContext.Coroutine == null)
				return JobResult.CompletedFailure;

			try
			{
				lock (LuaRuntime.GlobalLock)
				{
					var args = job.ScriptJobContext.YieldReturn;
					if (job.ScriptJobContext.Coroutine.State == CoroutineState.Dead)
						return JobResult.CompletedSuccess;

					var result = job.ScriptJobContext.Coroutine.Resume(args);

					if (job.ScriptJobContext.Coroutine.State == CoroutineState.Suspended)
						return JobResult.NotCompleted;
					else
						job.ScriptJobContext.YieldAnswer = result;
					return JobResult.CompletedSuccess;
				}
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
				LogManager.LogError("Runtime error during script execution: " + ex.Message);
				return JobResult.CompletedFailure;
			}
		}
	}
}
