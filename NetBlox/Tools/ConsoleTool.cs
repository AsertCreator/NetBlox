using NetBlox.Instances;
using NetBlox.Runtime;

namespace NetBlox.Tools
{
	public static class ConsoleTool
	{
		public const int SecurityLevel = 8;
		public static Thread? ConsoleThread;

		public static void Run()
		{
			ConsoleThread = new Thread(() =>
			{
				Console.WriteLine($"NetBlox Console, version {AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}");

				while (true)
				{
					try
					{
						Console.Write(">>> ");
						var cmd = Task.Run(() => Console.ReadLine());

						while (!cmd.IsCompleted)
						{
							if (GameManager.ShuttingDown) return;
						}

						var words = cmd.Result!.Split(' ');
						var cname = words[0];

						switch (cname)
						{
							case "shutdown":
								GameManager.MessageQueue.Enqueue(new()
								{
									Type = MessageType.Shutdown
								});
								break;
							case "insttree":
								if (GameManager.CurrentRoot == null)
								{
									Console.WriteLine("No DataModel yet!");
									break;
								}
								var c = DumpInst(GameManager.CurrentRoot, 0);
								Console.WriteLine("Total instances: " + c);
								break;
							case "teleport":
								GameManager.TeleportToServer(null);
								break;
							case "throwup":
								RenderManager.AutomaticThrowup = new Exception(cmd.Result[8..^0]);
								break;
							case "lua":
								LuaRuntime.RunScript(cmd.Result[4..^0], true, null, SecurityLevel, true);
								break;
							default:
								Console.WriteLine("No such command!");
								break;
						}
					}
					catch
					{
						Console.WriteLine("An error occurred while executing your command!");
					}
				}
			});
			ConsoleThread.Start();
		}
		public static int DumpInst(Instance inst, int tab)
		{
			int sum = 1;
			
			Console.WriteLine(new string(' ', tab * 4) + "\"" + inst.Name + "\", " + inst.ClassName);
			var children = inst.GetChildren();

			for (int i = 0; i < children.Length; i++)
			{
				sum += DumpInst(children[i], tab + 1);
			}

			return sum;
		}
	}
}
