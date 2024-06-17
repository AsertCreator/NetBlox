﻿using NetBlox.Common;
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
			if (uri.Contains(".."))
			{
				i = 403;
				return File.ReadAllText("./content/forbidden.html");
			}
			if (uri == "/") return File.ReadAllText("./content/index.html");
			if (uri.StartsWith("/cdn/")) // its not cdn actually but idc
			{
				return File.ReadAllText("./content/" + uri.Split('/')[2]);
			}
			if (uri.StartsWith("/game/"))
			{
				return File.ReadAllText("./content/gamepage.html");
			}
			if (uri == "/join" || uri == "/join/")
			{
				return File.ReadAllText("./content/joingame.html");
			}
			if (uri.StartsWith("/api/"))
			{
				return ServeAPI(cl, uri, ref i);
			}
			i = 404;
			return File.ReadAllText("./content/notfound.html");
		}
		private string? ServeAPI(HttpListenerContext cl, string uri, ref int i)
		{ 
			if (uri == "/api/login")
			{
				string data = cl.Request.InputStream.ReadToEnd();
				// idk what to do
			}
			return "{}";
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
					cl.Response.StatusCode = code;
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