using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

[Service]
public class CoreAuthenticationService : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool IsLoggedIn => GameManager.CloudConfiguration.UserId != 0;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool IsLoggedInAsGuest => GameManager.CloudConfiguration.UserId < 0;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public long CurrentUserId => GameManager.CloudConfiguration.UserId;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string CurrentUsername => GameManager.CloudConfiguration.Username ?? "";
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string CurrentAccessToken => GameManager.CloudConfiguration.ReadAccessToken() ?? "";

    public override string ClassName => nameof(CoreAuthenticationService);

    public const ulong NETWORK_CONSTANT_ID = 103;

    public CoreAuthenticationService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield LoginAs(string username, string password)
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
                GameManager.CloudConfiguration.AuthenticateAsUser(username, password);

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
                    task.ErrorOnReexecutionDetails = "Authentication failed";
                    task.IsWaiting = false;
                }
            }
        });

        return new LuaYield();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield LoginAsGuest()
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
                GameManager.CloudConfiguration.AuthenticateAsGuest();

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
                    task.ErrorOnReexecutionDetails = "Guest authentication failed";
                    task.IsWaiting = false;
                }
            }
        });

        return new LuaYield();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void Logout()
    {
        GameManager.CloudConfiguration.LogOut();
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(CoreAuthenticationService))
            return base.IsA(className);
        return true;
    }
}