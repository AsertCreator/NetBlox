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
		public static string PublicServiceName = "";
		public static bool IsReadonly = false;
		public static bool IsUnderMaintenance = false;

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

			WaitForAllServices();
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
		public static void StopAllServices()
		{
			Log.Information("Stopping all services...");
			for (int i = 0; i < Services.Count; i++)
				Services[i].Stop();
		}
		public static void WaitForAllServices() => Task.WaitAll((from x in Services select x.ServiceTask).ToArray());
	}
	public abstract class Service
	{
		public abstract string Name { get; }
		public bool IsRunning { get; protected set; }
		public Task ServiceTask { get; protected set; }

		public void Start()
		{
			IsRunning = true;
			ServiceTask = Task.Run(OnStart);
		}
		public void Stop()
		{
			IsRunning = false;
			OnStop();
		}
		protected abstract void OnStart();
		protected abstract void OnStop();
	}
}
