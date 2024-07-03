using NetBlox.Common;
using Serilog;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NetBlox.PublicService
{
	public class WebService : Service
	{
		public override string Name => nameof(WebService);
		private CancellationTokenSource TokenSource = new();
		private Task Task;
		private bool Running = false;

		private string? Serve(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			if (uri.Contains(".."))
			{
				i = 403;
				return File.ReadAllText("./content/forbidden.html");
			}

			if (uri == "/") return File.ReadAllText("./content/index.html");
			else if (uri.StartsWith("/check")) return $"";
			else if (uri.StartsWith("/cdn")) return ServeContent(cl, uri.Split('/')[2], ref i, ref mime);
			else if (uri.StartsWith("/game")) return ServeGamePage(long.Parse(uri.Substring(6))); 
			else if (uri.StartsWith("/join")) return File.ReadAllText("./content/joingame.html");
			else if (uri.StartsWith("/api"))  return ServeAPI(cl, uri, ref i, ref mime);

			i = 404;
			return File.ReadAllText("./content/notfound.html");
		}
		private string ServeGamePage(long gameid)
		{
			string raw = File.ReadAllText("./content/gamepage.html");
			raw = raw.Replace("$$GAMENAME$$", "Crossroads");
			return raw;
		}
		private string ServeContent(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			mime = "text/plain";
			uri = "content/" + uri;
			if (uri.Contains(".."))
			{
				i = 403;
				return "Forbidden";
			}
			if (!File.Exists(uri))
			{
				i = 404;
				return "Not found";
			}
			if (uri.EndsWith(".png"))
				mime = "image/png";
			if (uri.EndsWith(".jpg"))
				mime = "image/jpeg";
			if (uri.EndsWith(".css"))
				mime = "text/css";
			if (uri.EndsWith(".js"))
				mime = "text/javascript";
			if (uri.EndsWith(".html"))
			{
				i = 403;
				return "Forbidden";
			}

			return File.ReadAllText(uri);
		}
		private string? ServeAPI(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			try
			{
				mime = "application/json";
				string data = cl.Request.InputStream.ReadToEnd();

				string EncodeJson(Dictionary<string, object> enc) => JsonSerializer.Serialize(enc);

				if (uri == "/api/query/general") 
					return EncodeJson(new() {
						["placeCount"] = Program.GetService<PlaceService>().AllPlaces.Count,
						["userCount"] = Program.GetService<UserService>().AllUsers.Count,
						["name"] = Program.PSName
					});

				if (uri == "/api/users/exists")
					return EncodeJson(new()
					{
						["value"] = Program.GetService<UserService>().GetUserByID(long.Parse(data)) != null
					});
				if (uri == "/api/users/name") 
					return EncodeJson(new()
					{
						["value"] = Program.GetService<UserService>().GetUserByID(long.Parse(data))!.Name
					});
				if (uri == "/api/users/id")
					return EncodeJson(new()
					{
						["id"] = Program.GetService<UserService>().GetUserByName(data)!.Id
					});
				if (uri == "/api/users/login")
				{
					try
					{
						string[] credentials = data.Split('\n');
						User? user = Program.GetService<UserService>().Login(credentials[0], credentials[1]);
						if (user != null)
							return EncodeJson(new()
							{
								["id"] = user.Id,
								["token"] = user.CurrentLoginToken.ToString()
							});
						i = 400;
						return "No such user!";
					}
					catch (Exception ex)
					{
						i = 400;
						return ex.Message;
					}
				}
				if (uri == "/api/users/create") 
					return "{\"token\":\"" + Program.GetService<UserService>().CreateUser(data.Split('\n'), ref i) + "\"}";
				if (uri == "/api/users/getpresence") 
					return "{\"onlineMode\":\"" + Program.GetService<UserService>().GetUserByName(data)!.CurrentPresence.ToString() + "\"}";
				if (uri == "/api/users/setpresence") 
					return "{\"onlineMode\":\"" + Program.GetService<UserService>().SetPresence(data.Split('\n'), ref i) + "\"}";

				if (uri == "/api/places/exists") 
					return "{\"value\":" + ((Program.GetService<PlaceService>().GetPlaceByID(long.Parse(data)) != null) ? "true" : "false") + "}";
				if (uri == "/api/places/icon")
				{
					mime = "image/png";
					return Encoding.ASCII.GetString(File.ReadAllBytes(Program.GetService<PlaceService>().GetPlaceByID(long.Parse(data))!.IconFilePath));
				}
				if (uri == "/api/places/update/content") 
					return "{\"success\":" + (Program.GetService<PlaceService>().UpdatePlaceContent(data.Split('\n'), ref i) ? "true" : "false") + "}";
				if (uri == "/api/places/update/info") 
					return "{\"success\":" + Program.GetService<PlaceService>().UpdatePlaceInfo(data.Split('\n'), ref i) + "}";
				if (uri == "/api/places/create") 
					return "{\"id\":\"" + Program.GetService<PlaceService>().CreatePlace(data.Split('\n'), ref i) + "\"}";
				if (uri == "/api/places/shutdown")
					return "{\"success\":\"" + Program.GetService<PlaceService>().ShutdownPlaceServers(data.Split('\n'), ref i) + "\"}";
				if (uri == "/api/places/info")
				{
					var place = Program.GetService<PlaceService>().GetPlaceByID(long.Parse(data))!;
					return "{\"name\":\"" + place.Name + "\",\"author\":" + place.UserId + ",\"desc\":\"" +  + "\"}";
				}
				if (uri == "/api/places/random") 
					return "{\"id\":" + Program.GetService<PlaceService>().AllPlaces[Random.Shared.Next(0, Program.GetService<PlaceService>().AllPlaces.Count - 1)].Id + "}";

				i = 404;
				return "Not found";
			}
			catch
			{
				i = 500;
				return "Internal Server Error";
			}
		}
		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				HttpListener listener = new HttpListener();
				listener.Prefixes.Add("http://*:80/");
				listener.Start();

				Log.Information("WebService: Listening at port 80...");

				while (true)
				{
					var cl = await listener.GetContextAsync().AsCancellable(TokenSource.Token);
					var st = cl.Response.OutputStream;
					var by = new byte[0];

					TokenSource.Token.ThrowIfCancellationRequested();

					var code = 200;
					var mime = "text/html";
					try
					{
						var f = Serve(cl, cl.Request.Url.LocalPath, ref code, ref mime) ?? "";

						by = Encoding.UTF8.GetBytes(f);
						cl.Response.ContentType = mime;
						cl.Response.StatusCode = code;
						cl.Response.ContentLength64 = by.Length;
						st.Write(by);
						st.Flush();
						cl.Response.Close();
					}
					catch
					{
						code = 404;
						var f = File.ReadAllText("./content/notfound.html");

						by = Encoding.UTF8.GetBytes(f);
						cl.Response.ContentType = mime;
						cl.Response.StatusCode = code;
						cl.Response.ContentLength64 = by.Length;
						st.Write(by);
						st.Flush();
						cl.Response.Close();
					}

					TokenSource.Token.ThrowIfCancellationRequested();
				}
			});
			Running = true;
		}
		public override void Stop()
		{
			Running = false;
			base.Stop();
			TokenSource.Cancel();
		}
		public override bool IsRunning() => Running;
	}
}
