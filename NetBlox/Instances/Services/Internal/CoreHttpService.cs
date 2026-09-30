using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

[Service]
public class CoreHttpService : Instance
{
    public HttpClient? HttpClient;

    public override string ClassName => nameof(CoreHttpService);

    public const ulong NETWORK_CONSTANT_ID = 101;

    public CoreHttpService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        HttpClient = new HttpClient();
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield GetAsync(string url)
    {
        if (HttpClient == null)
            return new LuaYield();
        
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
                using HttpResponseMessage message = await HttpClient.GetAsync(url);
                string? content = await message.Content.ReadAsStringAsync();

                lock (task)
                {
                    Script script = context.GetLuaStateFor(task.Identity!);
                    Table table = new Table(script);

                    table["code"] = message.StatusCode;
                    table["content"] = content ?? "";

                    task.AsyncFunctionArguments = DynValue.NewTable(table);
                    task.IsWaiting = false; 
                }
            }
            catch
            {
                lock (task)
                {
                    task.ErrorOnReexecution = true;
                    task.ErrorOnReexecutionDetails = "Http GET request failure";
                    task.IsWaiting = false;
                }
            }
        });

        return new LuaYield();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public LuaYield PostAsync(string url, string body)
    {
        if (HttpClient == null)
            return new LuaYield();
        
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
                using StringContent postcontent = new StringContent(body);
                using HttpResponseMessage message = await HttpClient.PostAsync(url, postcontent);
                string? content = await message.Content.ReadAsStringAsync();

                lock (task)
                {
                    Script script = context.GetLuaStateFor(task.Identity!);
                    Table table = new Table(script);

                    table["code"] = message.StatusCode;
                    table["content"] = content ?? "";

                    task.AsyncFunctionArguments = DynValue.NewTable(table);
                    task.IsWaiting = false; 
                }
            }
            catch
            {
                lock (task)
                {
                    task.ErrorOnReexecution = true;
                    task.ErrorOnReexecutionDetails = "Http POST request failure";
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
        HttpClient?.Dispose();
        HttpClient = null;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(CoreHttpService))
            return base.IsA(className);
        return true;
    }
}