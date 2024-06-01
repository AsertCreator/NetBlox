using System.Net;

namespace NetBlox
{
	public static class Profile
	{
		public static string? Username;
		public static string? LoginToken;
		public static int UserId;
		public static int ApperanceId;
		public static bool IsTouchDevice;
		public static bool IsMouseDevice;
		public static bool IsGamepadDevice;
		public static IPAddress LoginServer;
		
		public static void Login(string user, string passw)
		{
			// todo: no
		}
		public static void SetOnlineMode(OnlineMode pm)
		{
			// todo: no
		}
	}
	public enum OnlineMode
	{
		Offline, OnlineOnWebsite, OnlineOnApp, InGame, InStudio, Banned
	}
}
