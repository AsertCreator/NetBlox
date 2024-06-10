using Serilog;
using System.Net;
using System.Text;

namespace NetBlox.PublicService
{
	/// <summary>
	/// Manages all servers running on local computer. Does not provide any kind of REST API, that's a job of <seealso cref="WebService"/>
	/// </summary>
	public class ServerService : Service
	{
		public override string Name => nameof(ServerService);
		private Task Task;
		private bool Running = false;

		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				Log.Information("ServerService: Successfully started!");
			});
			Running = true;
		}
		public override void Stop()
		{
			Running = false;
			base.Stop();
		}
		public override bool IsRunning() => Running;
	}
}
