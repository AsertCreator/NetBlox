﻿namespace NetBlox.Client
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Client ({GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}) is running...");
			GameManager.Start(true, false, args);
		}
	}
}