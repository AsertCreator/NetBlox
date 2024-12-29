using System.Net;
using System.Text;
using System.Text.Json;
using NetBlox.Common;
using Serilog;
using Version = NetBlox.Common.Version;

namespace NetBlox.PublicService
{
	public static class Program
	{
		public static string PublicServiceName = "";
		public static bool IsReadonly = false;
		public static bool IsUnderMaintenance = false;
		public static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
		{
			IncludeFields = true
		};

		public static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.CreateLogger();
			Log.Information($"Starting NetBlox public service (v{Version.VersionMajor}.{Version.VersionMinor}.{Version.VersionPatch})...");

			WebService webService = new WebService();
			webService.Listen();
		}
	}
}
