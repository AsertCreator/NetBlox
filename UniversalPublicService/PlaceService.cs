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
				Log.Information("PlaceService: Successfully started!");

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
