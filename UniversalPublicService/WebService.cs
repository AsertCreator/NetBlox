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

		private byte[] Serve(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			if (uri.Contains(".."))
			{
				i = 403;
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/forbidden.html"));
			}

			if (Program.IsUnderMaintenance)
			{
				if (uri.StartsWith("/res"))
					return ServeContent(cl, uri[5..], ref i, ref mime);
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/maintenance.html"));
			}

			if (uri == "/") 
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/index.html"));
			else if (uri.StartsWith("/check")) 
				return Encoding.UTF8.GetBytes($"");
			else if (uri.StartsWith("/res")) 
				return ServeContent(cl, uri[5..], ref i, ref mime);
			else if (uri.StartsWith("/game")) 
				return Encoding.UTF8.GetBytes(ServeGamePage(long.Parse(uri[6..]))); 
			else if (uri.StartsWith("/join")) 
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/joingame.html"));
			else if (uri.StartsWith("/login"))
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/login.html"));
			else if (uri.StartsWith("/search"))
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/search.html"));
			else if (uri.StartsWith("/api"))  
				return Encoding.UTF8.GetBytes(ServeAPI(cl, uri, ref i, ref mime)!);

			i = 404;
			return Encoding.UTF8.GetBytes(File.ReadAllText("./content/notfound.html"));
		}
		private string ServeGamePage(long gameid) => File.ReadAllText("./content/gamepage.html"); // dont really do much
		private byte[] ServeContent(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			mime = "text/plain";
			if (uri.Contains(".."))
			{
				i = 403;
				return Encoding.UTF8.GetBytes("Forbidden");
			}
			if (!File.Exists("./content/res/" + uri))
			{
				i = 404;
				return Encoding.UTF8.GetBytes("Not found");
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
				return Encoding.UTF8.GetBytes("Forbidden");
			}

			return File.ReadAllBytes("./content/res/" + uri);
		}
		private string? ServeAPI(HttpListenerContext cl, string uri, ref int i, ref string mime)
		{
			mime = "application/json";
			string data = APIProcessor.DispatchCall(cl, ref i);
			mime = cl.Request.ContentType!; // im so fucking tired idc atp
			return data;
		}
		protected override async void OnStart()
		{
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://+:80/");
			listener.Start();

			Log.Information("WebService: Listening at port 80...");

			while (IsRunning)
			{
				var cl = await listener.GetContextAsync().AsCancellable(TokenSource.Token);
				var st = cl.Response.OutputStream;
				var by = new byte[0];

				TokenSource.Token.ThrowIfCancellationRequested();

				var code = 200;
				var mime = "text/html";
				try
				{
					by = Serve(cl, cl.Request.Url!.LocalPath, ref code, ref mime);

					cl.Response.ContentType = mime;
					cl.Response.StatusCode = code;
					cl.Response.ContentLength64 += by.Length;
					st.Write(by);
					st.Flush();
					cl.Response.Close();
				}
				catch
				{
					code = 404;
					by = Encoding.UTF8.GetBytes(File.ReadAllText("./content/notfound.html"));

					cl.Response.ContentType = mime;
					cl.Response.StatusCode = code;
					cl.Response.ContentLength64 += by.Length;
					st.Write(by);
					st.Flush();
					cl.Response.Close();
				}

				TokenSource.Token.ThrowIfCancellationRequested();
			}
		}
		protected override void OnStop() => TokenSource.Cancel();
	}
}
