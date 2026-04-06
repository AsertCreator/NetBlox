using NetBlox.Instances;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NetBlox
{
	public class GameVariation
	{
		[JsonPropertyName("flagMap")]
		public Dictionary<string, bool>? FastFlags;
		[JsonPropertyName("intMap")]
		public Dictionary<string, int>? FastInts;
		[JsonPropertyName("stringMap")]
		public Dictionary<string, string>? FastStrings;
	}

	/// <summary>
	/// Provides some APIs for the whole NetBlox environment
	/// </summary>
	public static class AppManager
	{
		public static GameManager? CurrentGameManager;
		public static List<GameManager> GameManagers = [];
		public static RenderManager? CurrentRenderManager;
		public static Dictionary<string, string> Preferences = [];
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastInts = [];
		public static Action<string> PlatformOpenBrowser = x => { };
		public static HttpClient HttpClient = new();
		public static Job? GameRenderer;
		public static Job? GameProcessor;
		public static Job? GamePhysics;
		public static Job? GameGC;
		public static int PreferredFPS = 60;
		public static int PreferredPhysicsRate = 20;
		public static int PreferredNetworkRate = 20;
		public static bool EnablePeriodicGC = false;
		public static bool ShuttingDown = false;
		public static bool BlockReplication = false; // apparently moonsharp does not like the way im adding instances??
		public static string ContentFolder = Path.GetFullPath("./content/");
		public static string LibraryFolder = Path.GetFullPath("./tmp/");
		public static string PublicServiceAPI = "";
		public static DateTime WhenStartedRunning;
		public static event EventHandler<GameManager>? OnGameCreated;
		public static event EventHandler<EventArgs>? OnAppStarted;
		public static event EventHandler<EventArgs>? OnAppShutdown;
		public static int VersionMajor => Common.Version.VersionMajor;
		public static int VersionMinor => Common.Version.VersionMinor;
		public static int VersionPatch => Common.Version.VersionPatch;

		static AppManager()
		{
			LogManager.LogPrefixer = () => CurrentGameManager == null ? "<nogm>" : CurrentGameManager.ManagerName;
		}

		public static GameManager CreateGame(GameConfiguration gc, string[] args, Action<GameManager> loadcallback, Action<DataModel>? dmc = null)
		{
			GameManager manager = new(gc, args, loadcallback, dmc);
			GameManagers.Add(manager);
			LogManager.LogInfo($"Created new game manager \"{gc.GameName}\"...");
			OnGameCreated?.Invoke(null, manager);
			return manager;
		}
		public static void SetRenderTarget(GameManager gm) => CurrentRenderManager = gm.RenderManager;
		public static void SetPreference(string key, string val) => Preferences[key] = val;
		public static string GetPreference(string key) => Preferences[key];
		public static void Start()
		{
			if (!Directory.Exists(LibraryFolder))
				Directory.CreateDirectory(LibraryFolder);

			if (File.Exists("./gameVariation.json"))
			{
				try
				{
					var data = SerializationManager.DeserializeJson<GameVariation>(File.ReadAllText("./gameVariation.json"));
					if (data.FastFlags != null)
						FastFlags = data.FastFlags;
					if (data.FastInts!= null)
						FastInts = data.FastInts;
					if (data.FastStrings != null)
						FastStrings = data.FastStrings;
				}
				catch (Exception ex)
				{
					LogManager.LogError("Couldn't load game variation file, error: " + ex.GetType() + ", msg: " + ex.Message);
				}
			}

			GameProcessor = TaskScheduler.ScheduleNamedJob("Heartbeat", JobType.Heartbeat, x =>
			{
				for (int i = 0; i < GameManagers.Count; i++)
				{
					var gm = GameManagers[i];

					CurrentGameManager = gm;

					if (gm.IsRunning)
						gm.ProcessInstance(gm.CurrentRoot);
				}
				return JobResult.NotCompleted;
			});
			GameProcessor.JobTimingContext.Priority = 20;

			GamePhysics = TaskScheduler.ScheduleNamedJob("Physics", JobType.Physics, x =>
			{
				Stopwatch stopwatch = new();
				stopwatch.Start();

				for (int i = 0; i < GameManagers.Count; i++)
				{
					var gm = GameManagers[i];

					CurrentGameManager = gm;

					gm.PhysicsManager.Step();
				}

				stopwatch.Stop();

				var leftPhysicsTime = 1000 / PreferredPhysicsRate - stopwatch.Elapsed.TotalMilliseconds;
				if (leftPhysicsTime > 0)
					TaskScheduler.CurrentJob.JobTimingContext.JoinedUntil = DateTime.UtcNow.AddMilliseconds(leftPhysicsTime);

				return JobResult.NotCompleted;
			});
			GameRenderer = TaskScheduler.ScheduleNamedJob("Renderer", JobType.Renderer, x =>
			{
				if (CurrentRenderManager != null)
				{
					CurrentGameManager = CurrentRenderManager.GameManager;
					CurrentRenderManager.RenderFrame();
					return CurrentRenderManager.GameManager.ShuttingDown && CurrentRenderManager.GameManager.MainManager
						? JobResult.CompletedSuccess
						: JobResult.NotCompleted;
				}
				return JobResult.NotCompleted;
			});

			if (EnablePeriodicGC)
			{
				GameGC = TaskScheduler.ScheduleNamedJob("GarbageCollection", JobType.Miscellaneous, x =>
				{
					GC.Collect();
					CurrentGameManager = null;
					x.JobTimingContext.JoinedUntil = DateTime.UtcNow.AddSeconds(7);
					return JobResult.NotCompleted;
				});
			}

			WhenStartedRunning = DateTime.UtcNow;

			OnAppStarted?.Invoke(null, new());

			while (!ShuttingDown) TaskScheduler.Step();
		}
		public static void Shutdown()
		{
			for (int i = 0; i < GameManagers.Count; i++)
				GameManagers[i].Shutdown();
			ShuttingDown = true;
			OnAppShutdown?.Invoke(null, new());
			throw new RollbackException();
		}
		public static float GetRendererDeltaTime()
		{
			var span = GameRenderer.JobTimingContext.LastExecutionTime - GameRenderer.JobTimingContext.LastLastExecutionTime;
			return (float)span.TotalSeconds;
		}
		public static float GetProcessorDeltaTime()
		{
			var span = GameProcessor.JobTimingContext.LastTotalExecutionTime - 
				GameProcessor.JobTimingContext.LastLastTotalExecutionTime;
			return (float)span.TotalSeconds;
		}
		public static float DeltaFactor() => GetRendererDeltaTime() / GetProcessorDeltaTime();
		public static async Task<string> DownloadAssetAsync(long aid) =>
			await DownloadFileAsync(PublicServiceAPI + "/api/asset/get?aid=" + aid, PublicServiceAPI.GetHashCode() + "_" + aid + ".nas");
		public static async Task<string> DownloadFileAsync(string from, string to)
		{
			Directory.CreateDirectory("downloads");
			string path = "downloads/" + to;
			if (File.Exists(path))
				return path;
			using var stream = await HttpClient.GetStreamAsync(from);
			using var file = File.OpenWrite(path);

			try
			{
				await stream.CopyToAsync(file);
				file.Flush();
				return Path.GetFullPath(path).Replace('\\', '/');
			}
			finally
			{
				stream.Close();
				throw new Exception("Failed to download an asset from " + from + " to " + to + "!");
			}
		}
		/// <summary>
		/// Resolves a URL used within this whole game, always returns local path (downloads files from internet when necessary)
		/// </summary>
		/// <param name="url"></param>
		/// <param name="allowremote"></param>
		/// <returns></returns>
		public static async Task<string> ResolveUrlAsync(string url, bool allowremote, bool allowfiles = false)
		{
			url = url.TrimStart().TrimEnd();
			if (url.Contains("..")) return ""; // no
			else if ((url.StartsWith("http://") || url.StartsWith("https://")) && allowremote)
				return await DownloadFileAsync(url, url.GetHashCode() + ".ffl");
			else if (url.StartsWith("file://") && allowfiles)
				return url[7..];
			else if (url.StartsWith("rbxasset://"))
			{
				Uri uri = new(url);
				return long.TryParse(uri.LocalPath, out long assetid) && allowremote
					? await DownloadAssetAsync(assetid)
					: Path.Combine(Path.GetFullPath(ContentFolder), uri.Authority + uri.LocalPath).Replace('\\', '/');
			}
			return "";
		}
	}
	public class RollbackException : Exception { }
}
