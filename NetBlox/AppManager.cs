#define DISABLE_EME
using NetBlox.Instances;

namespace NetBlox
{
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
		public static Job? GameGC;
		public static int PreferredFPS = 60;
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

		/// <summary>
		/// Defines a fast flag, must be called after loading current fast flags
		/// </summary>
		public static void DefineFastFlag(string fflag, bool def)
		{
			if (!FastFlags.TryGetValue(fflag, out var _))
				FastFlags[fflag] = def;
		}
		public static void DefineFastInt(string fflag, int def)
		{
			if (!FastInts.TryGetValue(fflag, out var _))
				FastInts[fflag] = def;
		}
		public static void DefineFastString(string fflag, string def)
		{
			if (!FastStrings.TryGetValue(fflag, out var _))
				FastStrings[fflag] = def;
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

			DefineFastFlag("FFlagShowCoreGui", true);
			DefineFastFlag("FFlagShowAFSCacheReload", false);
			DefineFastInt("FIntDefaultUIVariant", 1);

			GameProcessor = TaskScheduler.ScheduleJob(JobType.Heartbeat, x =>
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
			GameRenderer = TaskScheduler.ScheduleJob(JobType.Renderer, x =>
			{
				if (CurrentRenderManager != null)
				{
					CurrentRenderManager.RenderFrame();
					return CurrentRenderManager.GameManager.ShuttingDown && CurrentRenderManager.GameManager.MainManager
						? JobResult.CompletedSuccess
						: JobResult.NotCompleted;
				}
				return JobResult.NotCompleted;
			});
			GameGC = TaskScheduler.ScheduleJob(JobType.Miscellaneous, x =>
			{
				GC.Collect();
				x.JobTimingContext.JoinedUntil = DateTime.UtcNow.AddSeconds(7);
				return JobResult.NotCompleted;
			});

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
			int b = stream.ReadByte();
			while (b != -1)
			{
				file.Write([(byte)b]);
				b = stream.ReadByte();
			}
			file.Flush();
			return Path.GetFullPath(path).Replace('\\', '/');
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
