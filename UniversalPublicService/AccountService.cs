using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBlox.PublicService
{
	public class AccountService : Service
	{
		public override string Name => nameof(AccountService);
		public TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(30);
		public List<Account> AllUsers = new();

		protected override void OnStart()
		{
			LoadDatabase();
			Log.Information("AccountService: Successfully started and loaded users: " + AllUsers.Count);

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
		public Account? RegisterNewUser(string usern, string passw)
		{
			lock (AllUsers)
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

				Account? acc = new();
				acc.Id = AllUsers.Count;
				acc.Name = usern;
				acc.LoginToken = Guid.NewGuid();
				SetPassword(acc, passw);

				Program.GetService<PlaceService>().CreatePlace(acc.Name + "'s place", "", acc);
				AllUsers.Add(acc);

				return acc;
			}
		}
		public void SetPassword(Account acc, string password)
		{
			byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
			string hashStr = string.Concat(hash.Select(x => x.ToString("X2")));

			acc.PasswordHash = hashStr;
		}
		public bool CheckPassword(Account acc, string password)
		{
			byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
			string hashStr = string.Concat(hash.Select(x => x.ToString("X2")));

			return acc.PasswordHash == hashStr;
		}
		public Account? GetUserByToken(Guid token) => (from x in AllUsers where x.LoginToken == token select x).FirstOrDefault();
		public Account? GetUserByName(string name) => (from x in AllUsers where x.Name == name select x).FirstOrDefault();
		public Account? GetUserByID(long id) => (from x in AllUsers where x.Id == id select x).FirstOrDefault();
		public void LoadDatabase()
		{
			if (File.Exists("users"))
				AllUsers = (JsonSerializer.Deserialize<Account[]>(File.ReadAllText("users"), Program.SerializerOptions) ?? []).ToList();
		}
		public void SaveDatabase()
		{
			File.WriteAllText("users", JsonSerializer.Serialize(AllUsers.ToArray(), Program.SerializerOptions));
		}
	}
	[Flags]
	public enum Privilege
	{
		CanBuyItems = 1, CanJoinGroups = 2, CanChat = 4, CanPlaySingleplayer = 8, CanPlayMultiplayer = 16, 
		CanCreateItems = 32, CanCreateGroups = 64, CanCreateGames = 128, CanChangeAppearance = 256,
		CanBuyTokens = 512, CanTransferTokens = 1024, CanPlayBetaPlaces = 2048, CanPlayClient = 4096,
		CanRateItems = 8196
	}
	/// <summary>
	/// This is data model for an account, it does not provide high level methods, see them in <seealso cref="AccountService"/>
	/// </summary>
	public class Account : IWebSubject
	{
		public long Id;
		public Privilege Privileges;
		public bool IsBlocked;
		public string Name = "";
		public string Email = "";
		public string About = "";
		public string PasswordHash = "";
		public string AppearanceData = "";
		public double Rating;
		public Guid LoginToken;
		public OnlineMode Presence;

		string IWebSubject.Name => Name;
		string IWebSubject.Description => About;
	}
}
