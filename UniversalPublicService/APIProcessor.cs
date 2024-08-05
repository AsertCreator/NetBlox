using NetBlox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetBlox.PublicService
{
	public static class APIProcessor
	{
		public static List<APIEndpoint> Endpoints = [];

		static APIProcessor()
		{
			string GetQueryData(HttpListenerContext ctx, string name)
			{
				var qs = ctx.Request.QueryString;
				return qs[name] ?? throw new InvalidOperationException();
			}

			AddEndpoint("/api/query/general", delegate (HttpListenerContext x, ref int code) 
			{
				return EncodeJson(new()
				{
					["placeCount"] = Program.GetService<PlaceService>().AllPlaces.Count,
					["userCount"] = Program.GetService<UserService>().AllUsers.Count,
					["name"] = Program.PublicServiceName
				});
			});

			AddEndpoint("/api/users/info", delegate (HttpListenerContext x, ref int code)
			{
				User user = Program.GetService<UserService>().GetUserByID(long.Parse(GetQueryData(x, "id")));

				if (user == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such user found",
						["errorCode"] = 404
					});
				}

				return EncodeJson(new()
				{
					["type"] = 0,
					["name"] = user.Name,
					["id"] = user.Id,
					["membership"] = user.MembershipType,
					["presence"] = (int)user.CurrentPresence,
				});
			});

			AddEndpoint("/api/users/login", delegate (HttpListenerContext x, ref int code)
			{
				User user = Program.GetService<UserService>().GetUserByName(GetQueryData(x, "name"));

				if (user == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such user found",
						["errorCode"] = 404
					});
				}

				string phash = GetQueryData(x, "phash");

				if (!user.HasPassword() || user.CheckPasswordHash(phash))
				{
					user.CurrentLoginToken = Guid.NewGuid();
					return EncodeJson(new()
					{
						["token"] = user.CurrentLoginToken.ToString()
					});
				}

				code = 401;
				return EncodeJson(new()
				{
					["errorText"] = "Not authorized",
					["errorCode"] = 401
				});
			});

			AddEndpoint("/api/users/create", delegate (HttpListenerContext x, ref int code)
			{
				string name = GetQueryData(x, "name").TrimStart().TrimEnd();
				UserService us = Program.GetService<UserService>();
				User user = us.GetUserByName(name);

				if (user != null)
				{
					code = 400;
					return EncodeJson(new()
					{
						["errorText"] = "Username is taken",
						["errorCode"] = 400
					});
				}

				string password = GetQueryData(x, "pplain");
				user = us.CreateUser(name, password);

				if (user == null)
				{
					code = 500;
					return EncodeJson(new()
					{
						["errorText"] = "Could not create the user",
						["errorCode"] = 500
					});
				}

				user.CurrentLoginToken = Guid.NewGuid();
				return EncodeJson(new()
				{
					["token"] = user.CurrentLoginToken.ToString()
				});
			});

			AddEndpoint("/api/users/setpresence", delegate (HttpListenerContext x, ref int code)
			{
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				user.CurrentPresence = (OnlineMode)int.Parse(GetQueryData(x, "val"));
				return EncodeJson(new()
				{
					["presence"] = (int)user.CurrentPresence
				});
			});

			AddEndpoint("/api/places/info", delegate (HttpListenerContext x, ref int code)
			{
				Place place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(GetQueryData(x, "id")));

				if (place == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such place found",
						["errorCode"] = 404
					});
				}

				User? author = Program.GetService<UserService>().GetUserByID(place.UserId);

				return EncodeJson(new()
				{
					["type"] = 1,
					["name"] = place.Name,
					["desc"] = place.Description,
					["authorid"] = place.UserId,
					["authorname"] = author != null ? author.Name : "Unknown",
					["id"] = place.Id,
				});
			});

			AddEndpoint("/api/places/icon", delegate (HttpListenerContext x, ref int code)
			{
				Place place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(GetQueryData(x, "id")));

				if (place == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such place found",
						["errorCode"] = 404
					});
				}

				return EncodeJson(new()
				{
					["iconUrl"] = place.IconURL
				});
			});

			AddEndpoint("/api/places/create", delegate (HttpListenerContext x, ref int code)
			{
				PlaceService ps = Program.GetService<PlaceService>();
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				string name = GetQueryData(x, "name");
				Place place = ps.CreatePlace(name, x.Request.InputStream.ReadToEnd(), user);

				return EncodeJson(new()
				{
					["id"] = place!.Id
				});
			});

			AddEndpoint("/api/places/join", delegate (HttpListenerContext x, ref int code)
			{
				PlaceService ps = Program.GetService<PlaceService>();
				ServerService ss = Program.GetService<ServerService>();
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));
				Place place = ps.GetPlaceByID(long.Parse(GetQueryData(x, "gid")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				Server server = ss.FindServer(user, place!);

				return EncodeJson(new()
				{
					["ip"] = server.ServerIP.ToString(),
					["port"] = server.ServerPort
				});
			});

			AddEndpoint("/api/places/update/content", delegate (HttpListenerContext x, ref int code)
			{
				PlaceService ps = Program.GetService<PlaceService>();
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				Place place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(GetQueryData(x, "id")));

				if (place == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such place found",
						["errorCode"] = 404
					});
				}

				place.SetContent(x.Request.InputStream.ReadToEnd());

				return EncodeJson(new()
				{
					["success"] = true
				});
			});

			AddEndpoint("/api/places/update/info", delegate (HttpListenerContext x, ref int code)
			{
				PlaceService ps = Program.GetService<PlaceService>();
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				Place place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(GetQueryData(x, "id")));

				if (place == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such place found",
						["errorCode"] = 404
					});
				}

				place.Name = GetQueryData(x, "name");
				place.Description = GetQueryData(x, "desc");

				return EncodeJson(new()
				{
					["success"] = true
				});
			});

			AddEndpoint("/api/places/shutdown", delegate (HttpListenerContext x, ref int code)
			{
				PlaceService ps = Program.GetService<PlaceService>();
				User user = Program.GetService<UserService>().GetUserByToken(Guid.Parse(GetQueryData(x, "token")));

				if (user == null)
				{
					code = 401;
					return EncodeJson(new()
					{
						["errorText"] = "Not authorized",
						["errorCode"] = 401
					});
				}

				Place place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(GetQueryData(x, "id")));

				if (place == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such place found",
						["errorCode"] = 404
					});
				}

				place.ShutdownServers();

				return EncodeJson(new()
				{
					["success"] = true
				});
			});

			AddEndpoint("/api/search", delegate (HttpListenerContext x, ref int code)
			{
				SearchService ss = Program.GetService<SearchService>();
				UserService us = Program.GetService<UserService>();
				
				int amount = int.Parse(GetQueryData(x, "amount"));
				string query = GetQueryData(x, "q");

				if (query.Trim().Length < 3)
				{
					code = 400;
					return EncodeJson(new()
					{
						["success"] = false
					});
				}

				var entries = ss.Search(query, amount);
				var infos = new List<Dictionary<string, object>>();

				for (int i = 0; i < entries.Length; i++)
				{
					var entry = entries[i];
					var place = entry as Place;
					var user = entry as User;

					if (place != null)
					{
						User? author = us.GetUserByID(place.UserId);
						infos.Add(new()
						{
							["type"] = 1,
							["name"] = place.Name,
							["desc"] = place.Description,
							["authorid"] = place.UserId,
							["authorname"] = author != null ? author.Name : "Unknown",
							["id"] = place.Id,
						});
					}
					if (user != null)
					{
						infos.Add(new()
						{
							["type"] = 0,
							["name"] = user.Name,
							["id"] = user.Id,
							["membership"] = user.MembershipType,
							["presence"] = (int)user.CurrentPresence,
						});
					}
				}

				return EncodeJson(new()
				{
					["success"] = true,
					["entries"] = infos.ToArray()
				});
			});
		}

		public static void AddEndpoint(APIEndpoint endpoint) => Endpoints.Add(endpoint);
		public static void AddEndpoint(string path, APIDelegate apid) => Endpoints.Add(new() { Path = path, Delegate = apid });
		public static APIEndpoint? GetEndpoint(string path) => (from x in Endpoints where x.Path == path select x).FirstOrDefault();
		public static string DispatchCall(HttpListenerContext ctx, ref int code)
		{
			try
			{
				var end = GetEndpoint(ctx.Request.Url!.LocalPath);
				if (end == null)
				{
					code = 404;
					return EncodeJson(new()
					{
						["errorText"] = "No such API endpoint",
						["errorCode"] = 404
					});
				}
				code = 200;
				string res = end.Delegate(ctx, ref code);
				if (code == 200)
					ctx.Response.ContentType = end.MimeType;

				return res;
			}
			catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
			{
				code = 400;
				return EncodeJson(new()
				{
					["errorText"] = "Invalid parameters",
					["errorCode"] = 400
				});
			}
			catch
			{
				code = 500;
				return EncodeJson(new()
				{
					["errorText"] = "API endpoint had failed",
					["errorCode"] = 500
				});
			}
		}
		public static string EncodeJson(Dictionary<string, object> dict) => JsonSerializer.Serialize(dict);
	}
	public class APIEndpoint
	{
		public string Path;
		public string MimeType;
		public APIDelegate Delegate;
	}
	public delegate string APIDelegate(HttpListenerContext ctx, ref int code);
}
