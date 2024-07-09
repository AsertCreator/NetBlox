using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBlox.PublicService
{
	public class PlaceService : Service
	{
		public override string Name => nameof(PlaceService);
		public TimeSpan AutoSaveInterval = TimeSpan.FromMinutes(30);
		public List<Place> AllPlaces = new();
		private Task Task;
		private bool Running = false;

		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				LoadDatabase();
				Log.Information("PlaceService: Successfully started and loaded places: " + AllPlaces.Count);

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
		public Place? GetPlaceByID(long id) => (from x in AllPlaces where x.Id == id select x).FirstOrDefault();
		public Place? CreatePlace(string name, string content, User user)
		{
			Place place = new();
			place.Id = AllPlaces.Count;
			place.Name = name;
			place.ContentFilePath = "place" + place.Id;
			place.UserId = user.Id;
			place.SetContent(content);

			AllPlaces.Add(place);
			return place;
		}
		public string CreatePlace(string[] data, ref int i)
		{
			Guid loginToken = Guid.Parse(data[0]);
			string name = data[1];
			string desc = data[2];

			User? user = Program.GetService<UserService>().GetUserByToken(loginToken);
			if (user == null)
			{
				i = 403;
				return "Forbidden";
			}
			Place place = new Place();
			place.Name = name;
			place.Description = desc;
			place.Id = AllPlaces.Count;
			place.UserId = user.Id;
			place.IconFilePath = "content/defaultPlace.png";
			AllPlaces.Add(place);
			return place.Id.ToString();
		}
		public bool UpdatePlaceContent(string[] data, ref int i)
		{
			Guid loginToken = Guid.Parse(data[0]);
			long pid = long.Parse(data[1]);
			string content = string.Join('\n', data.Skip(2));

			User? user = Program.GetService<UserService>().GetUserByToken(loginToken);
			if (user == null)
			{
				i = 403;
				return false;
			}
			Place? pl = (from x in AllPlaces where x.Id == pid select x).FirstOrDefault();
			if (pl == null)
			{
				i = 404;
				return false;
			}
			pl.SetContent(content);
			return true;
		}
		public bool UpdatePlaceInfo(string[] data, ref int i)
		{
			Guid loginToken = Guid.Parse(data[0]);
			long pid = long.Parse(data[1]);
			string name = data[2];
			string desc = string.Join('\n', data.Skip(3));

			User? user = Program.GetService<UserService>().GetUserByToken(loginToken);
			if (user == null)
			{
				i = 403;
				return false;
			}
			Place? pl = (from x in AllPlaces where x.Id == pid select x).FirstOrDefault();
			if (pl == null)
			{
				i = 404;
				return false;
			}
			pl.Name = name;
			pl.Description = desc;
			return true;
		}
		public bool ShutdownPlaceServers(string[] data, ref int i)
		{
			Guid loginToken = Guid.Parse(data[0]);
			long pid = long.Parse(data[1]);

			User? user = Program.GetService<UserService>().GetUserByToken(loginToken);
			if (user == null)
			{
				i = 403;
				return false;
			}
			Place? pl = (from x in AllPlaces where x.Id == pid select x).FirstOrDefault();
			if (pl == null)
			{
				i = 404;
				return false;
			}
			Program.GetService<ServerService>().ShutdownMatching(x => x.PlaceId == pl.Id);
			return true;
		}
		public void LoadDatabase()
		{
			if (File.Exists("places"))
				AllPlaces = JsonSerializer.Deserialize<Place[]>(File.ReadAllText("places"), new JsonSerializerOptions()
				{
					IncludeFields = true
				})!.ToList();
		}
		public void SaveDatabase()
		{
			File.WriteAllText("places", JsonSerializer.Serialize<Place[]>(AllPlaces.ToArray(), new JsonSerializerOptions()
			{
				IncludeFields = true
			}));
		}
	}
	public class Place
	{
		[JsonPropertyName("id")]
		public long Id;
		[JsonPropertyName("name")]
		public string Name = "";
		[JsonPropertyName("desc")]
		public string Description = "";
		[JsonPropertyName("content")]
		public string ContentFilePath = "";
		[JsonPropertyName("icon")]
		public string IconFilePath = "";
		[JsonPropertyName("uid")]
		public long UserId;

		public void SetContent(string content) => File.WriteAllText(ContentFilePath, content);
		public string GetContent() => File.ReadAllText(ContentFilePath);
	}
}
