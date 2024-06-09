using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.PublicService
{
	public class WebService : Service
	{
		public override string Name => nameof(WebService);
		private CancellationTokenSource TokenSource = new();
		private Task Task;
		private bool Running = false;

		private string? Serve(HttpListenerContext cl, string uri, ref int i)
		{
			if (uri == "/") return File.ReadAllText("./content/index.html");
			if (uri.StartsWith("/cdn/")) // its not cdn actually but idc
			{
				return File.ReadAllText("./content/" + uri.Split('/')[2]);
			}
			if (uri.StartsWith("/game/"))
			{
				return "fuck you";
			}
			if (uri == "/join" || uri == "/join/")
			{
				return File.ReadAllText("./content/joingame.html");
			}
			return File.ReadAllText("./content/notfound.html");
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

					Log.Information("Requested page " + cl.Request.Url.LocalPath);

					var code = 200;
					var f = Serve(cl, cl.Request.Url.LocalPath, ref code) ?? "";

					by = Encoding.UTF8.GetBytes(f);
					cl.Response.ContentLength64 = by.Length;
					st.Write(by);
					st.Flush();
					cl.Response.StatusCode = 200;
					cl.Response.Close();

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
