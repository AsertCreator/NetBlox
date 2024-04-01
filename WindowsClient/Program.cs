namespace NetBlox.Client
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox ({GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}) is running...");
			GameManager.Start(true, args);
		}
	}
}