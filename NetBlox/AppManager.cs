using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace NetBlox
{
	/// <summary>
	/// Provides some APIs for the whole NetBlox environment
	/// </summary>
	public static class AppManager
	{
		public static List<GameManager> GameManagers = new List<GameManager>();
		public static RenderManager? CurrentRenderManager;
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastNumbers = [];
		public static int PreferredFPS = 60;
		public static bool ShuttingDown = false;
		public static string ContentFolder = "content/";
		public const int VersionMajor = 2;
		public const int VersionMinor = 2;
		public const int VersionPatch = 0;

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
		public static void Start()
		{
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
						if (gm.CurrentRoot != null && gm.IsRunning && gm.ProhibitProcessing)
							gm.ProcessInstance(gm.CurrentRoot);
					}

					Schedule();
				}
				catch (RollbackException re)
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
