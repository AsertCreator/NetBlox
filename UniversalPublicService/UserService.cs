using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace NetBlox.PublicService
{
	public class UserService : Service
	{
		public override string Name => nameof(ServerService);
		public TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(30);
		public int AmountOfUsers => PageCount * PAGE_SIZE + LastPageSize;
		public const int PAGE_SIZE = 256;
		private Task Task;
		private bool Running = false;
		private int PageCount = 0;
		private int LastPageSize = 0;
		private List<User> CachedUsers = new();

		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				LoadDatabase();
				Log.Information("UserService: Successfully started!");

				while (Running)
				{
					lock (CachedUsers)
					{
						Thread.Sleep(AutoSaveInterval);
						SaveDatabase();
					}
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
		public User? GetUserByID(int id)
		{
			lock (CachedUsers)
			{
				var user = CachedUsers.Find(x => x.Id == id);
				if (user != null) return user;
				// we load users on demand
				var page = id / PAGE_SIZE;
				var str = File.OpenRead("./users/up" + page);

				using (BinaryReader br = new(str))
				{
					int uc = br.ReadInt32();
					if (uc > PAGE_SIZE)
					{
						Log.Error("User database's page #" + page + " is corrupted!");
						return null;
					}
					for (int i = 0; i < uc; i++)
					{
						int uid = br.ReadInt32();
						int mst = br.ReadInt32();
						bool isadmin = br.ReadBoolean();
						string name = br.ReadString();
						string email = br.ReadString();
						int aux = br.ReadInt32();
						byte[] hash = br.ReadBytes(256 / 8);

						if (uid == id)
						{
							User userobj = new()
							{
								Id = uid,
								MembershipType = mst,
								IsAdmin = isadmin,
								Name = name,
								Email = email,
								Auxilary = aux
							};
							userobj.SetPassword(hash);
							CachedUsers.Add(userobj);
							return user;
						}
					}
				}
				// we didn't find fortunately.
				return null;
			}
		}
		public User? RegisterUser(string name, string password)
		{
			lock (CachedUsers)
			{
				User user = new();
				user.Id = AmountOfUsers;
				user.Name = name;
				user.SetPassword(password);

				if (++LastPageSize > PAGE_SIZE)
					LastPageSize = 0;

				CachedUsers.Add(user);
				return user;
			}
		}
		public void LoadDatabase()
		{
			PageCount = Directory.GetFiles("./users/").Length;
			var str = File.OpenRead("./users/up" + (PageCount - 1));

			using (BinaryReader br = new(str))
				LastPageSize = br.ReadInt32();
		}
		public void SaveDatabase()
		{
			// no
		}
	}
	public class User
	{
		public int Id;
		public int MembershipType;
		public bool IsAdmin;
		public string Name = "";
		public string Email = "";
		public int Auxilary;
		private byte[]? PasswordHash;

		// idk why i am doing this
		public const int TYPE_BANNED = -1;
		public const int TYPE_NORMAL = 0;
		public const int TYPE_PREMIUM = 1;

		public void SetPassword(string password) => PasswordHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
		public void SetPassword(byte[] hash) => PasswordHash = hash;
		public bool HasPassword() => PasswordHash != null;
		public bool CheckPassword(string password)
		{
			var p1 = SHA256.HashData(Encoding.UTF8.GetBytes(password));
			var p0 = PasswordHash;
			return Enumerable.SequenceEqual(p1, p0);
		}
	}
}
