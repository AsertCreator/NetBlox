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

		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File($"{DateTime.Now:s}.log")
				.CreateLogger();
			Log.Information($"Starting NetBlox public service (v{Version.VersionMajor}.{Version.VersionMinor}.{Version.VersionPatch})...");
			GetService<ServerService>().Start();
			GetService<WebService>().Start();
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

		public virtual void Start() => Log.Information("Service " + Name + " is starting");
		public virtual void Stop() => Log.Information("Service " + Name + " is stopping");
		public abstract bool IsRunning();
	}
}
