using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.PublicService
{
	public class SearchService : Service
	{
		public override string Name => nameof(ServerService);

		protected override void OnStart()
		{
			Log.Information("SearchService: started successfully!");
			// we dont
		}
		protected override void OnStop()
		{
			// we dont
		}
		public ISearchable[] Search(string query, int amount) => Search(query.Split(' '), amount);
		public ISearchable[] Search(string[] keywords, int amount)
		{
			var places = Program.GetService<PlaceService>().AllPlaces.Cast<ISearchable>();
			var users = Program.GetService<UserService>().AllUsers.Cast<ISearchable>();
			var result = new List<ISearchable>();
			var all = places.Concat(users).ToArray();

			for (int i = 0; i < keywords.Length; i++)
				keywords[i] = keywords[i].Trim().ToLower();

			for (int i = 0; i < all.Length; i++)
			{
				var entry = all[i];

				for (int j = 0; j < keywords.Length; j++)
				{
					if (entry.Name.ToLower().Contains(keywords[j]) || entry.Description.ToLower().Contains(keywords[j]))
					{
						result.Add(entry);
						break;
					}
				}

				if (result.Count >= amount)
					break;
			}

			return result.ToArray();
		}
	}
	public interface ISearchable
	{
		public string Name { get; }
		public string Description { get; }
	}
}
