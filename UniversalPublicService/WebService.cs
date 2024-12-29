using NetBlox.Common;
using Serilog;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NetBlox.PublicService
{
	public class WebService
	{
		private bool IsRunning = true;
		private CancellationTokenSource TokenSource = new();

		private byte[] Serve(HttpListenerContext cl)
		{
			string uri = cl.Request.Url!.LocalPath;

			if (uri.Contains(".."))
			{
				cl.Response.StatusCode = 403;
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/forbidden.html"));
			}

			if (Program.IsUnderMaintenance)
			{
				if (uri.StartsWith("/res"))
					return ServeContent(cl);
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/maintenance.html"));
			}

			if (uri == "/") 
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/index.html"));
			else if (uri.StartsWith("/check")) 
				return Encoding.UTF8.GetBytes($"");
			else if (uri.StartsWith("/res")) 
				return ServeContent(cl);
			else if (uri.StartsWith("/game")) 
				return Encoding.UTF8.GetBytes(ServeGamePage(long.Parse(uri[6..]))); 
			else if (uri.StartsWith("/join")) 
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/joingame.html"));
			else if (uri.StartsWith("/login"))
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/login.html"));
			else if (uri.StartsWith("/search"))
				return Encoding.UTF8.GetBytes(File.ReadAllText("./content/search.html"));
			else if (uri.StartsWith("/api"))  
				return Encoding.UTF8.GetBytes(ServeAPI(cl)!);

			cl.Response.StatusCode = 404;
			return Encoding.UTF8.GetBytes(File.ReadAllText("./content/notfound.html"));
		}
		private string ServeGamePage(long gameid) => File.ReadAllText("./content/gamepage.html"); // dont really do much
		private byte[] ServeContent(HttpListenerContext cl)
		{
			string uri = cl.Request.Url!.LocalPath[5..];
			cl.Response.ContentType = "text/plain";

			if (uri.Contains(".."))
			{
				cl.Response.StatusCode = 403;
				return Encoding.UTF8.GetBytes("Forbidden");
			}
			if (!File.Exists("./content/res/" + uri))
			{
				cl.Response.StatusCode = 404;
				return Encoding.UTF8.GetBytes("Not found");
			}
			if (uri.EndsWith(".png"))
				cl.Response.ContentType = "image/png";
			if (uri.EndsWith(".jpg"))
				cl.Response.ContentType = "image/jpeg";
			if (uri.EndsWith(".css"))
				cl.Response.ContentType = "text/css";
			if (uri.EndsWith(".js"))
				cl.Response.ContentType = "text/javascript";
			if (uri.EndsWith(".html"))
			{
				cl.Response.StatusCode = 403;
				return Encoding.UTF8.GetBytes("Forbidden");
			}

			return File.ReadAllBytes("./content/res/" + uri);
		}
		private string? ServeAPI(HttpListenerContext cl)
		{
			cl.Response.ContentType = "application/json";
			string data = APIProcessor.DispatchCall(cl);
			cl.Response.ContentType = cl.Request.ContentType!; // im so fucking tired idc atp
			return data;
		}
		public async Task Listen()
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

				try
				{
					try
					{
						cl.Response.ContentType = "text/html";
						cl.Response.StatusCode = 200;

						by = Serve(cl);

						cl.Response.ContentLength64 += by.Length;
						st.Write(by);
						st.Flush();
						cl.Response.Close();
					}
					catch (FileNotFoundException)
					{
						by = Encoding.UTF8.GetBytes(File.ReadAllText("./content/notfound.html"));

						cl.Response.ContentType = "text/html";
						cl.Response.StatusCode = 404;
						cl.Response.ContentLength64 += by.Length;
						st.Write(by);
						st.Flush();
						cl.Response.Close();
					}
					catch
					{
						by = Encoding.UTF8.GetBytes(File.ReadAllText("./content/forbidden.html"));

						cl.Response.ContentType = "text/html";
						cl.Response.StatusCode = 500;
						cl.Response.ContentLength64 += by.Length;
						st.Write(by);
						st.Flush();
						cl.Response.Close();
					}

					TokenSource.Token.ThrowIfCancellationRequested();
				}
				catch (Exception ex)
				{
					// severe shit happened
					Log.Warning("Something weird happened in WebService: " + ex.GetType().FullName + ", msg: " + ex.Message);
				}
			}
		}
		public void StopListening() => IsRunning = false;
	}
}
