using System.Diagnostics;
using System.Text;

namespace NetBlox;

public class GameLogger
{
    public readonly GameManager GameManager;
    public readonly StringBuilder Log;

    private class GameLoggerTraceListener : TraceListener
    {
        public required GameLogger GameLogger;

        public override void Write(string? message)
        {
            if (!Debugger.IsAttached)
                Console.Write(message);
            GameLogger.Log.Append(message);
        }
        public override void WriteLine(string? message)
        {
            if (!Debugger.IsAttached)
                Console.WriteLine(message);
            GameLogger.Log.AppendLine(message);
        }
    }

    public GameLogger(GameManager gameManager)
    {
        GameManager = gameManager;
        Log = new StringBuilder();

        Trace.Listeners.Add(new GameLoggerTraceListener()
        {
            GameLogger = this
        });
    }

    public void SubmitTraces()
    {
        // TODO: submit traces
    }
}