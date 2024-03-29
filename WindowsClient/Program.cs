using NetBlox;

namespace NetBlox.Client
{
	internal static class Program
	{
		internal static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			AppManager.Start(true, "WindowsClient", args);
		}
	}
}