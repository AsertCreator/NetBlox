namespace UniversalInstaller
{
	internal class Program
	{
		static void Main(string[] args)
		{
			if (OperatingSystem.IsWindows())
			{
				Console.WriteLine("[.] Running on Windows, determining whether NetBlox is installed...");
				if (File.Exists("./NetBloxClient.exe"))
				{
					Console.WriteLine("[.] NetBlox is installed, checking its version...");

				}
			}
		}
	}
}
