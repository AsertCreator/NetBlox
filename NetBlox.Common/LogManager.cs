using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public static class LogManager
	{
		public static StringBuilder Log = new();
		public static bool IsBrowser = OperatingSystem.IsBrowser();
		public static event EventHandler<string>? OnLog;
		public static Func<string>? LogPrefixer;
		private static object loglock = new();

		public static void LogInfo(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-info] " + message;
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				OnLog?.Invoke(null, fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
		public static void LogWarn(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-warn] " + message;
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				OnLog?.Invoke(null, fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
		public static void LogError(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}]" + (LogPrefixer == null ? "" : '[' + LogPrefixer() + ']') + "[nb-error] " + message;
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				OnLog?.Invoke(null, fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
	}
}
