using NetBlox.Runtime;

namespace NetBlox;

public delegate SchedulerTaskResult SchedulerTaskDelegate(GameSchedulerTask self);
public delegate void SchedulerTaskWaitHandlerDelegate(GameSchedulerTask waiter, GameSchedulerTask waitee);
public enum SchedulerTaskResult
{
    NotCompleted, CompletedSuccess, CompletedFailed
}

public class GameSchedulerTask
{
    public required string DebugName;
    public required GameScheduler GameScheduler;
    public required SchedulerTaskDelegate Delegate;
    public GameScheduler.SchedulerPhase TargetPhase;
    public GlobalTimestamp WaitingTimeTarget;
    public GameSchedulerTask? WaitingTask;
    public SchedulerTaskWaitHandlerDelegate? WaitingTaskHandler;
    public List<GameSchedulerTask>? TasksYouShouldPutAfterMe;
    public SchedulerTaskResult Result;
    public SecurityIdentity? Identity;
    public bool CanRunOnOtherThreads;
    public bool IsWaiting;
    public int ElapsedMicroseconds;
    public int ReentrancyLevel;
    public object? UserData;

    public virtual SchedulerTaskResult Execute()
    {
        return Delegate(this);
    }

    public override string ToString()
    {
        return DebugName;
    }
}