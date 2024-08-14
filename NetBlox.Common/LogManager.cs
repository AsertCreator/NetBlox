using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public static class LogManager
	{
		public static StringBuilder Log = new();
		public static bool IsBrowser = OperatingSystem.IsBrowser();
		private static object loglock = new();

		public static void LogInfo(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}][nb-info] {message}";
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
		public static void LogWarn(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}][nb-warn] {message}";
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
		public static void LogError(string message)
		{
			lock (loglock)
			{
				string fm = $"[{DateTime.UtcNow:R}][nb-error] {message}";
				Log.AppendLine(fm);
				if (!IsBrowser)
					Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(fm);
				Debug.WriteLine(fm);
				if (!IsBrowser)
					Console.ResetColor();
			}
		}
	}
}
