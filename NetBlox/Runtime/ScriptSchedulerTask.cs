using System.Diagnostics;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;

namespace NetBlox.Runtime;

public class ScriptSchedulerTask : GameSchedulerTask
{
    public BaseScript? Self;
    public DynValue? RootFunction;
    public DynValue? RootCoroutine;
    public DynValue AsyncFunctionArguments = DynValue.Nil;

    public Action? CallbackAfterSuccessfulExecution;
    public Action? CallbackAfterFailedExecution;

    public bool ErrorOnReexecution = false;
    public string? ErrorOnReexecutionDetails = null;

    private string? associatedCode;

    public ScriptSchedulerTask(BaseScript? self, string code)
    {
        Self = self;
        associatedCode = code;
    }
    public ScriptSchedulerTask(BaseScript? self, DynValue function)
    {
        Self = self;
        RootFunction = function;
    }

    public override SchedulerTaskResult Execute()
    {
        Debug.Assert(Identity != null);

        if (!GameScheduler.GameManager.ScriptsRunning)
            return SchedulerTaskResult.NotCompleted;

        try
        {
            ScriptContext scriptContext = GameScheduler.GameManager.RootModel.GetService<ScriptContext>();
            MoonSharp.Interpreter.Script script = scriptContext.GetLuaStateFor(Identity);

            if (RootFunction == null && associatedCode != null)
            {
                if (Self != null)
                    RootFunction = script.LoadString(associatedCode, Self.LocalEnvironment, Self.GetFullName());
                else
                    RootFunction = script.LoadString(associatedCode, scriptContext.CreateNewEphemeralLocalEnvironment(), DebugName);
            }
            if (RootCoroutine == null)
                RootCoroutine = script.CreateCoroutine(RootFunction);
            
            RootCoroutine.Coroutine.AutoYieldCounter = -1;
            lock (this)
            {
                if (ErrorOnReexecution && ErrorOnReexecutionDetails != null)
                    throw new Exception(ErrorOnReexecutionDetails);
                AsyncFunctionArguments = RootCoroutine.Coroutine.Resume(AsyncFunctionArguments);
            }
            if (RootCoroutine.Coroutine.State == CoroutineState.Dead)
            {
                CallbackAfterSuccessfulExecution?.Invoke();
                return SchedulerTaskResult.CompletedSuccess;
            }

            return SchedulerTaskResult.NotCompleted;
        }
        catch (SyntaxErrorException syntaxError)
        {
            Trace.TraceError("Script syntax error: " + syntaxError.Message);
            CallbackAfterFailedExecution?.Invoke();
            return SchedulerTaskResult.CompletedFailed;
        }
        catch (ScriptRuntimeException runtimeError)
        {
            Trace.TraceError("Script error: " + runtimeError.Message);
            for (int i = 0; i < runtimeError.CallStack.Count; i++)
            {
                WatchItem watchItem = runtimeError.CallStack[i];
                Trace.TraceError("\tat " + watchItem.Location.ToLine + ":" + watchItem.Location.ToChar);
            }
            CallbackAfterFailedExecution?.Invoke();
            return SchedulerTaskResult.CompletedFailed;
        }
        catch (Exception otherError)
        {
            Trace.TraceError("Script error: " + otherError.Message);
            CallbackAfterFailedExecution?.Invoke();
            return SchedulerTaskResult.CompletedFailed;
        }
    }
}