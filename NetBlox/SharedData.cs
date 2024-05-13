using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace NetBlox
{
	public static class SharedData
	{
		public static List<GameManager> GameManagers = new List<GameManager>();
		public static Dictionary<string, bool> FastFlags = [];
		public static Dictionary<string, string> FastStrings = [];
		public static Dictionary<string, int> FastNumbers = [];
		public static int PreferredFPS = 60;
		public static bool ShuttingDown = false;
		public static string ContentFolder = "content/";
		public const int VersionMajor = 2;
		public const int VersionMinor = 2;
		public const int VersionPatch = 0;

		public static GameManager CreateGame(string name, bool client, bool server, bool render, string[] args, Action<string, GameManager> callback)
		{
			GameManager manager = new GameManager();
			manager.ManagerName = name;
			manager.Start(client, server, render, args, callback);
			GameManagers.Add(manager);
			LogManager.LogInfo($"Created new game manager \"{name}\"...");
			return manager;
		}
		public static void StartTaskScheduler()
		{
			Task.Run(() =>
			{
				while (!ShuttingDown)
				{
					// perform processing
					for (int i = 0; i < GameManagers.Count; i++)
					{
						var gm = GameManagers[i];
						if (gm.CurrentRoot != null && gm.IsRunning)
							gm.ProcessInstance(gm.CurrentRoot);
					}

					Schedule();

					Thread.Sleep(1000 / 60);
				}
			});
		}
		public static void Shutdown()
		{
			for (int i = 0; i < GameManagers.Count; i++)
				GameManagers[i].Shutdown();
			ShuttingDown = true;
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

								if (thread.ScrInst != null)
									thread.Script.Globals["script"] = LuaRuntime.MakeInstanceTable(thread.ScrInst, thread.GameManager);
								else
									thread.Script.Globals["script"] = DynValue.Nil;

								var res = thread.Coroutine.Resume();
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
}
