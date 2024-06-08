// #define DISABLE_EME
using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System.Runtime;

namespace NetBlox
{
	/// <summary>
	/// Provides some APIs for the whole NetBlox environment
	/// </summary>
	public static class AppManager
	{
		public static List<GameManager> GameManagers = [];
		public static RenderManager? CurrentRenderManager;
		public static Dictionary<string, string> Preferences = [];
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastNumbers = [];
		public static int PreferredFPS = 60;
		public static bool ShuttingDown = false;
		public static string ContentFolder = "content/";
		public static string LibraryFolder = "tmp/";
		public static int VersionMajor => Common.Version.VersionMajor;
		public static int VersionMinor => Common.Version.VersionMinor;
		public static int VersionPatch => Common.Version.VersionPatch;

		public static void LoadFastFlags(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i]) 
				{
					case "--fast-flag":
						{
							var key = args[++i];
							var bo = int.Parse(args[++i]) == 1;

							FastFlags[key] = bo;
							LogManager.LogInfo($"Setting fast flag {key} to {bo}");
							break;
						}
					case "--fast-string":
						{
							var key = args[++i];
							var st = args[++i];

							FastStrings[key] = st;
							LogManager.LogInfo($"Setting fast stirng {key} to \"{st}\"");
							break;
						}
					case "--fast-number":
						{
							var key = args[++i];
							var nu = int.Parse(args[++i]);

							FastNumbers[key] = nu;
							LogManager.LogInfo($"Setting fast number {key} to {nu}");
							break;
						}
				}
			}
		}
		public static GameManager CreateGame(GameConfiguration gc, string[] args, Action<string, GameManager> callback)
		{
			GameManager manager = new GameManager();
			manager.ManagerName = gc.GameName;
			manager.Start(gc, args, callback);
			GameManagers.Add(manager);
			LogManager.LogInfo($"Created new game manager \"{gc.GameName}\"...");
			return manager;
		}
		public static void SetRenderTarget(GameManager gm)
		{
			CurrentRenderManager = gm.RenderManager;
		}
		public static void SetPreference(string key, string val) 
		{
			Preferences[key] = val;
		}
		public static string GetPreference(string key)
		{
			return Preferences[key];
		}
		public static void Start()
		{
			if (!Directory.Exists(LibraryFolder))
				Directory.CreateDirectory(LibraryFolder);

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

			while (!ShuttingDown)
			{
				try
				{
					if (CurrentRenderManager != null)
						CurrentRenderManager.RenderFrame();

					// perform processing
					for (int i = 0; i < GameManagers.Count; i++)
					{
						var gm = GameManagers[i];
						if (gm.CurrentRoot != null && gm.IsRunning && !gm.ProhibitProcessing)
						{
							gm.ProcessInstance(gm.CurrentRoot);
							if (gm.NetworkManager.IsClient)
								gm.PhysicsManager.Step();
						}
					}

					Schedule();
				}
				catch (RollbackException)
				{
					// a rollback happened
				}
			}
		}
		public static void Shutdown()
		{
			for (int i = 0; i < GameManagers.Count; i++)
				GameManagers[i].Shutdown();
			ShuttingDown = true;
			throw new RollbackException();
		}
		public static void Schedule()
		{
			if (LuaRuntime.CurrentThread != null)
			{
				if (LuaRuntime.CurrentThread.Value.Coroutine == null)
					LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
				else if (DateTime.Now >= LuaRuntime.CurrentThread.Value.WaitUntil &&
					LuaRuntime.CurrentThread.Value.Coroutine.State != CoroutineState.Dead)
				{
					var cst = new CancellationTokenSource();
#if !DISABLE_EME
#pragma warning disable SYSLIB0046 // Type or member is obsolete
					var tsk = Task.Run(() =>
					{
						ControlledExecution.Run(() =>
						{
#endif
							LuaRuntime.ReportedExecute(() =>
							{
								if (LuaRuntime.CurrentThread == null)
								{
									if (LuaRuntime.Threads.Count > 0)
										LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
									else
										return;
								}

								var thread = LuaRuntime.CurrentThread.Value;

								if (thread.GameManager.ProhibitScripts)
								{
									LuaRuntime.CurrentThread = LuaRuntime.CurrentThread.Next;
									return;
								}

								if (thread.ScrInst != null)
									thread.Script.Globals["script"] = LuaRuntime.MakeInstanceTable(thread.ScrInst, thread.GameManager);
								else
									thread.Script.Globals["script"] = DynValue.Nil;

								var res = thread.Coroutine.Resume(thread.StartArgs);
								if (thread.Coroutine.State != CoroutineState.Dead || res == null)
									return;
								else
								{
									if (LuaRuntime.Threads.Contains(thread))
									{
										var ac = thread.FinishCallback;
										if (ac != null) ac();
										LuaRuntime.Threads.Remove(LuaRuntime.CurrentThread);
									}
								}
							}, true);
#if !DISABLE_EME
#pragma warning restore SYSLIB0046 // Type or member is obsolete
						}, cst.Token);
					});
					if (!tsk.Wait(LuaRuntime.ScriptExecutionTimeout * 1000))
					{
						LuaRuntime.PrintError("Exhausted maximum script execution time!");
						var ac = LuaRuntime.CurrentThread.Value.FinishCallback;
						if (ac != null) ac();
						cst.Cancel();
					}
#endif
				}

				if (LuaRuntime.CurrentThread == null)
				{
					if (LuaRuntime.Threads.Count > 0)
						LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
					else
						return;
				}

				if (LuaRuntime.Threads.Count > 0)
					LuaRuntime.CurrentThread = LuaRuntime.CurrentThread.Next;
				else
					LuaRuntime.CurrentThread = null;
			}
			else
			{
				LuaRuntime.CurrentThread = LuaRuntime.Threads.First;
			}
		}
		public static string ResolveUrl(string url) => ContentFolder + url.Split("//")[1];
	}
	public class RollbackException : Exception { }
}
