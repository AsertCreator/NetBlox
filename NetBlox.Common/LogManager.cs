using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public struct LogMessage
	{
		public string ConstructedMessage;
		public bool IsWarning;
		public bool IsError;
	}
	public static class LogManager
	{
		public static StringBuilder Log = new();
		public static bool IsBrowser = OperatingSystem.IsBrowser();
		public static event EventHandler<LogMessage>? OnLog;
		public static Func<string>? LogPrefixer;
		public static Queue<LogMessage> MessagePostQueue = [];
		private static object loglock = new();
		private static Thread? LogThread = null;

		static LogManager()
		{
			void LogThreadMain()
			{
				while (true)
				{
					while (MessagePostQueue.Count == 0)
						Thread.Yield();
					LogMessage fm = MessagePostQueue.Dequeue();

					if (!IsBrowser)
					{
						if (!fm.IsWarning && !fm.IsError)
							Console.ForegroundColor = ConsoleColor.White;
						if (fm.IsError)
							Console.ForegroundColor = ConsoleColor.Red;
						if (fm.IsWarning)
							Console.ForegroundColor = ConsoleColor.Yellow;
					}

					OnLog?.Invoke(null, fm);
					Debug.WriteLine(fm.ConstructedMessage);
					Console.WriteLine(fm.ConstructedMessage);

					if (!IsBrowser)
					{
						Console.ResetColor();
					}
				}
			}

			LogThread = new Thread(LogThreadMain);
			LogThread.Start();
		}

		public static void LogInfo(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-info] " + message;
				LogMessage logmessage = new();
				logmessage.ConstructedMessage = fm;

				Log.AppendLine(fm);
				MessagePostQueue.Enqueue(logmessage);
			}
		}
		public static void LogWarn(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-warn] " + message;
				LogMessage logmessage = new();
				logmessage.ConstructedMessage = fm;
				logmessage.IsWarning = true;

				Log.AppendLine(fm);
				MessagePostQueue.Enqueue(logmessage);
			}
		}
		public static void LogError(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-error] " + message;
				LogMessage logmessage = new();
				logmessage.ConstructedMessage = fm;
				logmessage.IsError = true;

				Log.AppendLine(fm);
				MessagePostQueue.Enqueue(logmessage);
			}
		}
	}
}
