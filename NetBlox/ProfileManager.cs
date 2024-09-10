using NetBlox.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text;

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
		public Guid? LastLogin;

		/// <summary>
		/// Returns login token and sets it in <seealso cref="LastLogin"/>, if Public Service allowed to login such way, or null if login failed.
		/// </summary>
		public async Task<Guid?> LoginAsync(string user, string phash)
		{
			try
			{
				LogManager.LogInfo("Trying to login with " + user + "...");
				IsOffline = true;
				Dictionary<string, string> str = new();
				str["name"] = user;
				str["phash"] = phash;

				string json = SerializationManager.SerializeJson(str);
				var hc = new HttpClient();
				var res = await hc.PostAsync(AppManager.PublicServiceAPI + "/api/users/login/", new StringContent(json));
				var data = await res.Content.ReadAsStringAsync();
				var result = SerializationManager.DeserializeJson<Dictionary<string, string>>(data)!;

				if (res.StatusCode != HttpStatusCode.OK)
				{
					LogManager.LogError("Could not login, possibly wrong credentials, msg: " + result["errorText"]);
					LastLogin = null; // also dispose last login because why not
					return null;
				}
				else
				{
					Username = user;
					IsOffline = false;
					return Guid.Parse(result["token"]);
				}
			}
			catch
			{
				LogManager.LogError("Could not login, socket could not be opneded or something else happened");
				LastLogin = null; // also dispose last login because why not
				return null;
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
			try
			{
				if (!LastLogin.HasValue) return false;

				LogManager.LogInfo("Trying to set online mode to " + pm + "...");
				Dictionary<string, object> str = new();
				str["token"] = LastLogin.Value.ToString();
				str["val"] = (int)pm;

				string json = SerializationManager.SerializeJson(str);
				var hc = new HttpClient();
				var res = await hc.PostAsync(AppManager.PublicServiceAPI + "/api/users/setpresence/", new StringContent(json));

				if (res.StatusCode != HttpStatusCode.OK)
				{
					LogManager.LogError("Could not set online mode!");
					return false;
				}
				else return true;
			}
			catch
			{
				LogManager.LogError("Could not set online mode, socket could not be opneded or something else happened");
				return false;
			}
		}
		public async Task<bool> SetPlayerDataAsync(int dataid, byte[] data)
		{
			try
			{
				if (!LastLogin.HasValue) return false;

				LogManager.LogInfo("Trying to set player data...");
				Dictionary<string, object> str = new();
				str["token"] = LastLogin.Value.ToString();
				str["dataid"] = dataid;
				str["data"] = data;

				string json = SerializationManager.SerializeJson(str);
				var hc = new HttpClient();
				var res = await hc.PostAsync(AppManager.PublicServiceAPI + "/api/users/setplayerdata/", new StringContent(json));

				if (res.StatusCode != HttpStatusCode.OK)
				{
					LogManager.LogError("Could not set player data!");
					return false;
				}
				else return true;
			}
			catch
			{
				LogManager.LogError("Could not set player data, socket could not be opneded or something else happened");
				return false;
			}
		}
		public async Task<byte[]?> GetPlayerDataAsync(int dataid)
		{
			try
			{
				if (!LastLogin.HasValue) return null;

				LogManager.LogInfo("Trying to get player data...");
				Dictionary<string, object> str = new();
				str["token"] = LastLogin.Value.ToString();
				str["dataid"] = dataid;

				string json = SerializationManager.SerializeJson(str);
				var hc = new HttpClient();
				var res = await hc.PostAsync(AppManager.PublicServiceAPI + "/api/users/getplayerdata/", new StringContent(json));

				if (res.StatusCode != HttpStatusCode.OK)
				{
					LogManager.LogError("Could not get player data!");
					return null;
				}
				else return await res.Content.ReadAsByteArrayAsync();
			}
			catch
			{
				LogManager.LogError("Could not get player data, socket could not be opneded or something else happened");
				return null;
			}
		}
	}
}
