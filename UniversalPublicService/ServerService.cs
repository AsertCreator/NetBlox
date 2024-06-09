using Serilog;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace NetBlox.PublicService
{
	public class ServerService : Service
	{
		public override string Name => nameof(ServerService);
		private WebSocketServer Server;
		private Task Task;
		private bool Running = false;

		private class ServerServiceBeh  : WebSocketBehavior
		{
			protected override void OnMessage(MessageEventArgs e)
			{
				Log.Error(e.Data);
				Send("{}");
				Close();
			}
		}
		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				Server = new WebSocketServer("ws://localhost:443/");
				Server.AddWebSocketService<ServerServiceBeh>("/");
				Server.Start();
				Log.Information("ServerService: Listening at port 443...");
			});
			Running = true;
		}
		public override void Stop()
		{
			Running = false;
			Server.Stop();
			base.Stop();
		}
		public override bool IsRunning() => Running;
	}
}
