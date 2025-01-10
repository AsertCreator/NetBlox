using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using NetBlox;

namespace NetBloxDebug
{
	public static class Program
	{
		public static Thread DebugThread;
		public static Queue<Action> DebugThreadActions = [];

		public static void Main()
		{
			LogManager.LogInfo("Debugger was loaded successfully!");

			DebugThread = new Thread(() =>
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				while (true)
				{
					while (DebugThreadActions.Count > 0)
						DebugThreadActions.Dequeue()();
					Application.DoEvents();
				}
			});

			DebugThread.Start();

			AppManager.OnGameCreated += (_, gm) => DebuggerAddGame(gm);

			NetBlox.Client.Program.Main(Environment.GetCommandLineArgs());
		}
		public static void DebuggerAddGame(GameManager gm)
		{
			DebugThreadActions.Enqueue(() =>
			{
				GameDebugForm gdf = new GameDebugForm(gm);
				gdf.Show();
				gdf.Activate();
			});
		}
	}
}
