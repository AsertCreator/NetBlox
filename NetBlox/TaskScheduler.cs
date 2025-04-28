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
	}
	public class Job(JobType type, JobDelegate callback, int security)
	{
		public JobType Type = type;
		public int SecurityLevel = security;
		public JobDelegate NativeCallback = callback;
		public ScriptJobContext ScriptJobContext = new();
		public JobTimingContext JobTimingContext = new();
		public JobResult Result;
	}
	public enum JobType { Replication, Renderer, Heartbeat, Miscellaneous, Script }
	public enum JobResult { CompletedSuccess, CompletedFailure, NotCompleted }
	public static class TaskScheduler
	{
		public static Job CurrentJob;
		public static bool Enabled = true;
		public static TimeSpan LastCycleTime = TimeSpan.Zero;
		public static int JobCount => RunningJobs.Count;
		public static double AverageTimeToRun => LastCycleTime.TotalMilliseconds / RunningJobs.Count;
		internal static List<Job> RunningJobs = [];
		private static int PlayerTrust = 2;

		public static void Step()
		{
			Stopwatch sw = new();
			sw.Start();

			var now = DateTime.UtcNow.Ticks;

			for (int i = 0; i < RunningJobs.Count; i++)
			{
				var job = RunningJobs[i];

				if (job == null)
				{
					RunningJobs.RemoveAt(i--);
					// just skip it
					continue;
				}

				if (job.ScriptJobContext.GameManager != null) // god help me
				{
					if ((job.SecurityLevel == 7 || job.SecurityLevel == 8) && job.ScriptJobContext.GameManager.NetworkManager.IsClientGame)
					{
						LogManager.LogError("Server-exclusive script threads are not expected on client!");
						PlayerTrust--;
					}
				}

				if (PlayerTrust <= 0)
				{
					var _ = "Player trust is too low";
				}

				if (job.JobTimingContext.JoinedUntil.Ticks > now)
					continue;
				if (job.JobTimingContext.JoinedTo != null && job.JobTimingContext.JoinedTo.Result == JobResult.NotCompleted)
					continue;
				if (job.JobTimingContext.TaskJoinedTo != null && !job.JobTimingContext.TaskJoinedTo.IsCompleted)
					continue;

				CurrentJob = job;
				try
				{
					Stopwatch taswa = new();
					taswa.Start();

					var res = job.NativeCallback(job);
					job.JobTimingContext.HadRunBefore = true;
					job.Result = res;

					if (res != JobResult.NotCompleted)
					{
						job.ScriptJobContext.AfterDone?.Invoke(job);
						Terminate(job);
						i--;
					}

					taswa.Stop();
					job.JobTimingContext.LastCycleTime = taswa.ElapsedMilliseconds;
					if (res != JobResult.NotCompleted)
						break;
				}
				catch (Exception ex)
				{
					LogManager.LogError("Job execution error:" + ex.Message + "; the job will be terminated");
					Terminate(job);
					i--;
				}
			}

			sw.Stop();

			LastCycleTime = sw.Elapsed;
		}
		public static void Terminate(Job job) => RunningJobs.Remove(job);
		public static void Delay(int milliseconds, Action<Job> act)
		{
			Task.Delay(milliseconds).ContinueWith(_ =>
			{
				Job job = null!;
				job = Schedule(() =>
				{
					act(job);
				});
			});
		}
		public static Job Schedule(Action act)
		{
			return ScheduleJob(JobType.Miscellaneous, x =>
			{
				try
				{
					act();
					return JobResult.CompletedSuccess;
				}
				catch
				{
					return JobResult.CompletedFailure;
				}
			});
		}
		public static Job ScheduleJob(JobType type, JobDelegate jd, JobDelegate? afterDone = null, int level = 8)
		{
			Job job = new(type, jd, level);
			job.ScriptJobContext.AfterDone = afterDone;
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

					["script"] = LuaRuntime.PushInstance(self, gm),
					["workspace"] = LuaRuntime.PushInstance(gm.CurrentRoot.GetService<Workspace>(true), gm),
					["Workspace"] = LuaRuntime.PushInstance(gm.CurrentRoot.GetService<Workspace>(true), gm),

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
			Coroutine? closure = null;
			if (func.Type == DataType.Function) closure = gm.MainEnvironment.CreateCoroutine(func).Coroutine;
			else if (func.Type == DataType.Thread) closure = func.Coroutine;
			else throw new InvalidOperationException("Cannot create a thread with not a function or coroutine");

			var job = new Job(JobType.Script, ScriptJob, level);
			job.ScriptJobContext.GameManager = gm;
			job.ScriptJobContext.AfterDone = afterDone;
			job.ScriptJobContext.BaseScript = self;
			job.ScriptJobContext.YieldReturn = args ?? [];
			job.ScriptJobContext.Coroutine = closure;

			RunningJobs.Add(job);
			return job;
		}
		private static JobResult ScriptJob(Job job)
		{
			if (job.ScriptJobContext.Coroutine == null)
				return JobResult.CompletedFailure;

			try
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
