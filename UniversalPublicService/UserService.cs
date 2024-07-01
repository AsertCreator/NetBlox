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
		private Task Task;
		private bool Running = false;

		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				LoadDatabase();
				Log.Information("UserService: Successfully started!");

				AppDomain.CurrentDomain.ProcessExit += (x, y) =>
				{
					SaveDatabase();
				};

				while (Running)
				{
					Thread.Sleep(AutoSaveInterval);
					SaveDatabase();
				}
			});
			Running = true;
		}
		public override void Stop()
		{
			SaveDatabase();
			Running = false;
			base.Stop();
		}
		public override bool IsRunning() => Running;
		public string CreateUser(string[] data, ref int code)
		{
			string usern = data[0];
			string passw = data[1];

			if (GetUserByName(usern) != null)
			{
				code = 400;
				return "Username already taken";
			}

			for (int i = 0; i < usern.Length; i++)
			{
				char ch = usern[i];
				if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '.') { }
				else
				{
					code = 400;
					return "Invalid username";
				}
			}

			User? user = new();
			user.Id = AllUsers.Count;
			user.Name = usern;
			user.SetPassword(passw);
			user.CurrentLoginToken = Guid.NewGuid();
			AllUsers.Add(user);
			return user.CurrentLoginToken.ToString();
		}
		public string Login(string[] data, ref int code)
		{
			string usern = data[0];
			string phash = data[1];
			User? user = GetUserByName(usern);
			if (user == null)
			{
				code = 400;
				return "Could not login";
			}
			if (!user.CheckPasswordHash(phash))
			{
				code = 400;
				return "Could not login";
			}
			user.CurrentLoginToken = Guid.NewGuid();
			return user.CurrentLoginToken.ToString();
		}
		public string SetPresence(string[] data, ref int code)
		{
			Guid token = Guid.Parse(data[0]);
			User? user = GetUserByToken(token);
			if (user == null)
			{
				code = 400;
				return "Could not set presence";
			}
			user.CurrentPresence = (OnlineMode)int.Parse(data[1]);
			return user.CurrentPresence.ToString();
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
	public class User
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
		public OnlineMode CurrentPresence;

		// idk why i am doing this
		public const int TYPE_BANNED = -1;
		public const int TYPE_NORMAL = 0;
		public const int TYPE_PREMIUM = 1;
		public const int TYPE_ADMIN = 2;

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
