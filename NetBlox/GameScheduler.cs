using System.Diagnostics;
using NetBlox.Runtime;

namespace NetBlox;

public record struct GameSchedulerPerfEntry
{
    public GameSchedulerTask? Task;
    public float TimeUsed;
}

public sealed class GameScheduler
{
    public enum SchedulerPhase
    {
        Rendering, Physics, Network, Any
    }

    public List<GameSchedulerTask> SchedulerTasks = [];
    public GameSchedulerTask? CurrentSchedulerTask { get; private set; } = null!;
    public int LastIdleMicrosecondsElapsed;
    public bool PreferUncapped = false;

    public readonly GameManager GameManager;

    private SecurityIdentity? securityOverride;
    private string? securityOverrideExplanation;
    private object currentSchedulerTaskLock = new object();
    private Thread schedulerThread = Thread.CurrentThread;

    private List<GameSchedulerTask> immediateExecutionBuffer = new List<GameSchedulerTask>();

    public GameScheduler(GameManager gameManager)
    {
        GameManager = gameManager;
    }

    public GameSchedulerTask Schedule(string debugName, SchedulerPhase phase, SchedulerTaskDelegate action)
    {
        lock (this)
        {
            GameSchedulerTask task = new GameSchedulerTask()
            {
                DebugName = debugName,
                GameScheduler = this,
                Delegate = action,
                TargetPhase = phase,
                Identity = GetCurrentSecurityIdentity()
            };
            SchedulerTasks.Add(task);
            return task;
        }
    }
    public GameSchedulerTask Schedule(GameSchedulerTask schedulerTask)
    {
        lock (this)
        {
            schedulerTask.Identity = GetCurrentSecurityIdentity();
            SchedulerTasks.Add(schedulerTask);
            return schedulerTask;
        }
    }
    public GameSchedulerTask ScheduleForImmediateExecution(string debugName, SchedulerPhase phase, SchedulerTaskDelegate action)
    {
        lock (this)
        {
            GameSchedulerTask task = new GameSchedulerTask()
            {
                DebugName = debugName,
                GameScheduler = this,
                Delegate = action,
                TargetPhase = phase,
                Identity = GetCurrentSecurityIdentity()
            };
            SchedulerTasks.Add(task);
            immediateExecutionBuffer.Add(task);
            return task;
        }
    }
    public GameSchedulerTask ScheduleForImmediateExecution(GameSchedulerTask schedulerTask)
    {
        lock (this)
        {
            schedulerTask.Identity = GetCurrentSecurityIdentity();
            SchedulerTasks.Add(schedulerTask);
            immediateExecutionBuffer.Add(schedulerTask);
            return schedulerTask;
        }
    }
    public void BeginTracedSecurityOverride(SecurityIdentity choice, string explanation)
    {
        if (Thread.CurrentThread != schedulerThread)
            throw new InvalidOperationException("Cannot call BeginTracedSecurityOverride not on the main thread");

        Trace.TraceInformation("BTSO: \"" + explanation + "\" for " + choice.Name + " (" + choice.SecurityIdentityNumber + ")");
        securityOverride = choice;
        securityOverrideExplanation = explanation;
    }
    public void EndTracedSecurityOverride()
    {
        if (Thread.CurrentThread != schedulerThread)
            throw new InvalidOperationException("Cannot call EndTracedSecurityOverride not on the main thread");
        if (securityOverride == null)
            throw new InvalidOperationException("Cannot EndTracedSecurityOverride when not overriding");

        SecurityIdentity choice = securityOverride;
        Trace.TraceInformation("ETSO: \"" + securityOverrideExplanation + "\" for " + choice.Name + " (" + choice.SecurityIdentityNumber + ")");
        securityOverride = null;
    }
    public SecurityIdentity GetCurrentSecurityIdentity()
    {
        if (securityOverride != null)
            return securityOverride;
        
        lock (currentSchedulerTaskLock)
        {
            if (CurrentSchedulerTask == null)
                return SecurityIdentity.SI_Anonymous;

            SecurityIdentity? identity = CurrentSchedulerTask.Identity;
            if (!SecurityIdentity.AllowedSecurityIdentites.Contains(identity))
                CurrentSchedulerTask.Identity = SecurityIdentity.SI_Anonymous;

            return identity ?? SecurityIdentity.SI_Anonymous;
        }
    }
    public void EnterRunLoop()
    {
        Trace.TraceInformation("Starting GameScheduler...");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        while (GameManager.SchedulerRunning)
        {
            bool ranNoTasks = true;
            ulong closestWaitingTime = ulong.MaxValue;

            GameSchedulerTask[] immutable;
            lock (this)
            {
                immutable = new GameSchedulerTask[SchedulerTasks.Count];
                SchedulerTasks.CopyTo(immutable);
            }

            for (int i = 0; i < immutable.Length; i++)
            {
                GameSchedulerTask schedulerTask = immutable[i];

                lock (currentSchedulerTaskLock)
                    CurrentSchedulerTask = schedulerTask;

                stopwatch.Reset();
                stopwatch.Start();

                if (schedulerTask.Result != SchedulerTaskResult.NotCompleted)
                    continue;

                lock (schedulerTask)
                    if (schedulerTask.IsWaiting)
                        continue;

                if (schedulerTask.WaitingTask is not null)
                {
                    if (schedulerTask.WaitingTask.Result == SchedulerTaskResult.NotCompleted)
                        continue;
                    schedulerTask.WaitingTaskHandler?.Invoke(schedulerTask, schedulerTask.WaitingTask);
                    schedulerTask.WaitingTaskHandler = null;
                }
                schedulerTask.WaitingTask = null;

                if (schedulerTask.WaitingTimeTarget >= GameManager.CurrentGlobalTimestamp())
                {
                    if (closestWaitingTime > schedulerTask.WaitingTimeTarget)
                        closestWaitingTime = schedulerTask.WaitingTimeTarget;
                    continue;
                }

                ranNoTasks = false;
                SchedulerTaskResult result = schedulerTask.Execute();
                schedulerTask.Result = result;

                if (immediateExecutionBuffer.Count > 0)
                {
                    List<GameSchedulerTask> newImmutable = [.. immutable, .. immediateExecutionBuffer];
                    immutable = newImmutable.ToArray();
                    immediateExecutionBuffer.Clear();
                }
                if (schedulerTask.TasksYouShouldPutAfterMe != null)
                {
                    List<GameSchedulerTask> newImmutable =
                    [
                        .. immutable[0..(i+1)],
                        .. schedulerTask.TasksYouShouldPutAfterMe,
                        .. immutable[(i+1)..],
                    ];
                    immutable = newImmutable.ToArray();
                    schedulerTask.TasksYouShouldPutAfterMe = null;
                }

                if (result != SchedulerTaskResult.NotCompleted)
                    lock (this)
                        SchedulerTasks.Remove(schedulerTask);

                securityOverride = null;

                stopwatch.Stop();

                schedulerTask.ElapsedMicroseconds = (int)stopwatch.Elapsed.TotalMicroseconds;

                if (!GameManager.SchedulerRunning)
                    break;
            }

            lock (currentSchedulerTaskLock)
                CurrentSchedulerTask = null!;

            if (ranNoTasks)
            {
                long microseconds = (long)closestWaitingTime - (long)GameManager.CurrentGlobalTimestamp() - 100;
                if (microseconds > 0)
                {
                    LastIdleMicrosecondsElapsed = (int)microseconds;
                    Thread.Sleep((int)(microseconds / 1000));
                }
            }
        }

        Trace.TraceInformation("Stopping GameScheduler...");
    }
    public int ReadPerformance(GameSchedulerPerfEntry[] entry)
    {
        entry[0].Task = null;
        if (LastIdleMicrosecondsElapsed > 50)
            entry[0].TimeUsed = LastIdleMicrosecondsElapsed;

        for (int i = 0; i < SchedulerTasks.Count; i++)
        {
            entry[i + 1].Task = SchedulerTasks[i];
            entry[i + 1].TimeUsed = SchedulerTasks[i].ElapsedMicroseconds;
        }

        return SchedulerTasks.Count + 1;
    }
}