using NetBlox.Common;
using System.Net;

namespace NetBlox
{
	public class ProfileManager
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
		public string LoginUrl => "http://" + LoginServer.ToString() + ":80";

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
			var res = await hc.PostAsync(LoginUrl + "/api/users/login/", new StringContent(json));

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
			if (!LastLogin.HasValue) return false;

			LogManager.LogInfo("Trying to set online mode to " + pm + "...");
			Dictionary<string, object> str = new();
			str["version"] = $"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}";
			str["token"] = LastLogin.Value.ToString();
			str["online"] = (int)pm;

			string json = SerializationManager.SerializeJson(str);
			var hc = new HttpClient();
			var res = await hc.PostAsync(LoginUrl + "/api/users/setpresence/", new StringContent(json));

			if (res.StatusCode != HttpStatusCode.OK)
			{
				LogManager.LogError("Could not set online mode!");
				return false;
			}
			else return true;
		}
		public async Task<bool> SetPlayerDataAsync(int dataid, byte[] data)
		{
			if (!LastLogin.HasValue) return false;

			LogManager.LogInfo("Trying to set player data...");
			Dictionary<string, object> str = new();
			str["version"] = $"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}";
			str["token"] = LastLogin.Value.ToString();
			str["dataid"] = dataid;
			str["data"] = data;

			string json = SerializationManager.SerializeJson(str);
			var hc = new HttpClient();
			var res = await hc.PostAsync(LoginUrl + "/api/users/setplayerdata/", new StringContent(json));

			if (res.StatusCode != HttpStatusCode.OK)
			{
				LogManager.LogError("Could not set player data!");
				return false;
			}
			else return true;
		}
		public async Task<byte[]?> GetPlayerDataAsync(int dataid)
		{
			if (!LastLogin.HasValue) return null;

			LogManager.LogInfo("Trying to get player data...");
			Dictionary<string, object> str = new();
			str["version"] = $"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}";
			str["token"] = LastLogin.Value.ToString();
			str["dataid"] = dataid;

			string json = SerializationManager.SerializeJson(str);
			var hc = new HttpClient();
			var res = await hc.PostAsync(LoginUrl + "/api/users/getplayerdata/", new StringContent(json));

			if (res.StatusCode != HttpStatusCode.OK)
			{
				LogManager.LogError("Could not get player data!");
				return null;
			}
			else return await res.Content.ReadAsByteArrayAsync();
		}
	}
}
