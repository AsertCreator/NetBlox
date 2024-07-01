using System.Net;
using System.Text;
using NetBlox.Common;
using Serilog;
using Version = NetBlox.Common.Version;

namespace NetBlox.PublicService
{
	public static class Program
	{
		public static List<Service> Services = new();
		public static string PSName = "";

		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.CreateLogger();
			Log.Information($"Starting NetBlox public service (v{Version.VersionMajor}.{Version.VersionMinor}.{Version.VersionPatch})...");

			GetService<ServerService>();
			GetService<PlaceService>();
			GetService<UserService>();
			GetService<WebService>();

			WaitForAll();
		}
		public static T GetService<T>() where T : Service, new()
		{
			for (int i = 0; i < Services.Count; i++)
			{
				if (Services[i] is T)
					return (T)Services[i];
			}
			T s = new();
			s.Start();
			Services.Add(s);
			return s;
		}
		public static void StopAll()
		{
			Log.Information("Stopping all services...");
			for (int i = 0; i < Services.Count; i++)
				Services[i].Stop();
		}
		public static void WaitForAll()
		{
			while (true) // help me
			{
				bool stop = true;
				for (int i = 0; i < Services.Count; i++)
				{
					if (Services[i].IsRunning())
					{
						stop = false;
						break;
					}
				}
				if (stop)
					return;
			}
		}
	}
	public abstract class Service
	{
		public abstract string Name { get; }

		public virtual void Start() { }
		public virtual void Stop() { }
		public abstract bool IsRunning();
	}
}
