using NetBlox.Common;
using System.Net;

namespace NetBlox
{
	public class Profile
	{
		public string Username = "Unauthorized";
		public long UserId = -1;
		public long ApperanceId = -1;
		public bool IsTouchDevice;
		public bool IsMouseDevice;
		public bool IsGamepadDevice;
		public bool IsOffline = true;
		public IPAddress LoginServer = IPAddress.Any;
		public Guid? LastLogin;
		public string LoginUrl => "http://" + LoginServer.ToString() + ":455";

		/// <summary>
		/// Returns login token and sets it in <seealso cref="LastLogin"/>, if Public Service allowed to login such way, or null if login failed.
		/// </summary>
		public async Task<Guid?> LoginAsync(string user, string passw)
		{
			LogManager.LogInfo("Trying to login with " + user + "...");
			IsOffline = true;
			Dictionary<string, string> str = new();
			str["version"] = $"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}";
			str["user"] = user;
			str["pass"] = passw;

			string json = SerializationManager.SerializeJson(str);
			var hc = new HttpClient();
			var res = await hc.PostAsync(LoginUrl, new StringContent(json));

			if (res.StatusCode != HttpStatusCode.OK)
			{
				LogManager.LogError("Could not login, possibly wrong credentials");
				LastLogin = null; // also dispose last login because why not
				return null;
			}
			else
			{
				string data = await res.Content.ReadAsStringAsync();
				Dictionary<string, string> result = SerializationManager.DeserializeJson<Dictionary<string, string>>(data)!;
				Username = user;
				IsOffline = false;
				return Guid.Parse(result["token"]);
			}
		}
		public void LoginAsGuest()
		{
			IsOffline = true;
			LastLogin = null;
			Username = "Guest " + Random.Shared.Next(100, 9999);
		}
		public async Task<bool> SetOnlineModeAsync(OnlineMode pm)
		{
			if (LastLogin == null) return false;

			LogManager.LogInfo("Trying to set online mode to " + pm + "...");
			Dictionary<string, object> str = new();
			str["version"] = $"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}";
			str["token"] = LastLogin.ToString();
			str["online"] = (int)pm;

			string json = SerializationManager.SerializeJson(str);
			var hc = new HttpClient();
			var res = await hc.PostAsync(LoginUrl, new StringContent(json));

			if (res.StatusCode != HttpStatusCode.OK)
			{
				LogManager.LogError("Could not set online mode!");
				return false;
			}
			else return true;
		}
	}
}
