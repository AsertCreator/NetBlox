using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public static class LogManager
	{
		public static StringBuilder Log = new();
		public static bool IsBrowser = OperatingSystem.IsBrowser();
		public static event EventHandler<string>? OnLog;
		private static object loglock = new();
		private static string ProcessName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);

		public static void LogInfo(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}][{ProcessName}][inf] {message}";
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
				string fm = $"[{DateTime.UtcNow:R}][{ProcessName}][war] {message}";
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
				string fm = $"[{DateTime.UtcNow:R}][{ProcessName}][err] {message}";
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
