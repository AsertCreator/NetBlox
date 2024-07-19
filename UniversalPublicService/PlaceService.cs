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

		protected override void OnStart()
		{
			LoadDatabase();
			Log.Information("PlaceService: Successfully started and loaded places: " + AllPlaces.Count);

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
	public class Place : ISearchable
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
		public string IconURL = "";
		[JsonPropertyName("uid")]
		public long UserId;

		string ISearchable.Name => Name;
		string ISearchable.Description => Description;

		public void ShutdownServers()
		{
			var ss = Program.GetService<ServerService>();
			ss.ShutdownMatching(x => x.PlaceId == Id);
		}
		public void SetContent(string content) => File.WriteAllText(ContentFilePath, content);
		public string GetContent() => File.ReadAllText(ContentFilePath);
	}
}
