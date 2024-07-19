using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBlox.PublicService
{
	public class UserService : Service
	{
		public override string Name => nameof(UserService);
		public TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(30);
		public List<User> AllUsers = new();

		protected override void OnStart()
		{
			LoadDatabase();
			Log.Information("UserService: Successfully started and loaded users: " + AllUsers.Count);

			AppDomain.CurrentDomain.ProcessExit += (x, y) =>
			{
				SaveDatabase();
			};

			while (IsRunning)
			{
				Thread.Sleep(AutoSaveInterval);
				SaveDatabase();
			}
		}
		protected override void OnStop()
		{
			SaveDatabase();
		}
		public User? CreateUser(string usern, string passw)
		{
			if (GetUserByName(usern) != null)
				throw new Exception("Username already taken");

			for (int i = 0; i < usern.Length; i++)
			{
				char ch = usern[i];
				if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '.') { }
				else
				{
					throw new Exception("Invalid username format");
				}
			}

			User? user = new();
			user.Id = AllUsers.Count;
			user.Name = usern;
			user.SetPassword(passw);
			user.CurrentLoginToken = Guid.NewGuid();
			Program.GetService<PlaceService>().CreatePlace(user.Name + "'s place", "", user);
			AllUsers.Add(user);
			return user;
		}
		public User? GetUserByToken(Guid token) => (from x in AllUsers where x.CurrentLoginToken == token select x).FirstOrDefault();
		public User? GetUserByName(string name) => (from x in AllUsers where x.Name == name select x).FirstOrDefault();
		public User? GetUserByID(long id) => (from x in AllUsers where x.Id == id select x).FirstOrDefault();
		public User? RegisterUser(string name, string password)
		{
			User user = new();
			user.Id = AllUsers.Count;
			user.Name = name;
			user.SetPassword(password);

			AllUsers.Add(user);
			return user;
		}
		public void LoadDatabase()
		{
			if (File.Exists("users"))
				AllUsers = JsonSerializer.Deserialize<User[]>(File.ReadAllText("users"), new JsonSerializerOptions()
				{
					IncludeFields = true
				})!.ToList();
		}
		public void SaveDatabase()
		{
			File.WriteAllText("users", JsonSerializer.Serialize<User[]>(AllUsers.ToArray(), new JsonSerializerOptions()
			{
				IncludeFields = true
			}));
		}
	}
	public class User : ISearchable
	{
		[JsonPropertyName("id")]
		public long Id;
		[JsonPropertyName("mtype")]
		public int MembershipType;
		[JsonPropertyName("name")]
		public string Name = "";
		[JsonPropertyName("email")]
		public string Email = "";
		[JsonPropertyName("phash")]
		public string PasswordHash;
		[JsonIgnore]
		public Guid CurrentLoginToken;
		[JsonIgnore]
		public OnlineMode CurrentPresence 
		{ 
			get 
			{
				if (MembershipType == TYPE_BANNED)
					return OnlineMode.Banned;
				return presence;
			} 
			set => presence = value; 
		}
		private OnlineMode presence;

		// idk why i am doing this
		public const int TYPE_BANNED = -1;
		public const int TYPE_NORMAL = 0;
		public const int TYPE_PREMIUM = 1;
		public const int TYPE_ADMIN = 2;

		string ISearchable.Name => Name;
		string ISearchable.Description => "";

		public void SetPassword(string password) => PasswordHash = string.Join("", SHA256.HashData(Encoding.UTF8.GetBytes(password)));
		public void SetPassword(byte[] hash) => PasswordHash = string.Join("", hash);
		public bool HasPassword() => PasswordHash != null;
		public bool CheckPassword(string password)
		{
			var p1 = string.Join("", SHA256.HashData(Encoding.UTF8.GetBytes(password)));
			var p0 = PasswordHash;
			return p1 == p0;
		}
		public bool CheckPasswordHash(string hash) => PasswordHash == hash;
	}
}
