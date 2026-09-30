using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

[Service]
public class CoreFileService : Instance
{
    public override string ClassName => nameof(CoreFileService);

    public const ulong NETWORK_CONSTANT_ID = 102;

    public CoreFileService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield WriteText(string filePathInContainer, string content)
    {
        ScriptSchedulerTask? task = GameManager.GameScheduler.CurrentSchedulerTask as ScriptSchedulerTask;
        if (task == null)
            throw new InvalidOperationException("Cannot run async methods without a script task context");

        ScriptContext context = Root.GetService<ScriptContext>();
        
        lock (task)
            task.IsWaiting = true;

        Task.Run(async () =>
        {
            try
            {
                await File.WriteAllTextAsync(filePathInContainer, content);

                lock (task)
                {
                    task.AsyncFunctionArguments = DynValue.True;
                    task.IsWaiting = false; 
                }
            }
            catch
            {
                lock (task)
                {
                    task.ErrorOnReexecution = true;
                    task.ErrorOnReexecutionDetails = "Writing file failed";
                    task.IsWaiting = false;
                }
            }
        });

        return new LuaYield();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield ReadText(string filePathInContainer)
    {
        ScriptSchedulerTask? task = GameManager.GameScheduler.CurrentSchedulerTask as ScriptSchedulerTask;
        if (task == null)
            throw new InvalidOperationException("Cannot run async methods without a script task context");

        ScriptContext context = Root.GetService<ScriptContext>();
        
        lock (task)
            task.IsWaiting = true;

        Task.Run(async () =>
        {
            try
            {
                string text = await File.ReadAllTextAsync(filePathInContainer);

                lock (task)
                {
                    task.AsyncFunctionArguments = DynValue.NewString(text);
                    task.IsWaiting = false; 
                }
            }
            catch
            {
                lock (task)
                {
                    task.ErrorOnReexecution = true;
                    task.ErrorOnReexecutionDetails = "Reading file failed";
                    task.IsWaiting = false;
                }
            }
        });

        return new LuaYield();
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(CoreFileService))
            return base.IsA(className);
        return true;
    }
}