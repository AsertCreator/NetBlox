using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Server;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Server
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Server ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			/*GameManager.Start(false, true, false, args, x =>
			{
				DataModel dm = new();
				RbxlParser.Load(x, Root);

				Root = dm;

				LuaRuntime.Setup(Root, false);

				NetworkManager.StartServer();

				GameManager.IsRunning = true;
			});
			return;*/
			var g = AppManager.CreateGame(new()
			{
				AsServer = true,
				DoNotRenderAtAll = true, // how many times did i flip this switch on and off and on and off and on and off and o
				SkipWindowCreation = true,
				GameName = "NetBlox Server"
			}, args, (x) =>
			{
				try
				{
					if (x.ServerStartupInfo == null)
						Environment.Exit(1);

					x.CurrentRoot.Clear();
					x.CurrentIdentity.PlaceName = x.ServerStartupInfo.PlaceName;
					x.CurrentIdentity.UniverseName = x.ServerStartupInfo.UniverseName;
					x.CurrentIdentity.MaxPlayerCount = (uint)x.ServerStartupInfo.MaxPlayerCount;
					x.CurrentIdentity.Author = x.ServerStartupInfo.PlaceAuthor;
					x.CurrentRoot.Name = x.CurrentIdentity.PlaceName;
					x.CurrentRoot.InternalLoad("file://" + x.ServerStartupInfo.RbxlFilePath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Could not load the place: " + ex.Message);
					Environment.Exit(1);
				}

				if (File.Exists("gamestart.txt"))
					TaskScheduler.ScheduleScript(x, File.ReadAllText("gamestart.txt"), 8, null);

				Task.Run(x.NetworkManager.StartServer);
				Task.Run(() =>
				{
					Console.WriteLine("NetBlox Server commmand line:");
					while (!x.ShuttingDown)
					{
						try
						{

							Console.Write(">> ");

							string cmd = Console.ReadLine();
							if (cmd == null) continue;

							string[] words = cmd.Split(' ');
							if (words.Length == 0) continue;

							switch (words[0])
							{
								case "load":
									try
									{
										x.CurrentRoot.Clear();
										x.CurrentIdentity.PlaceName = "Place downloaded from Web";
										x.CurrentIdentity.UniverseName = "NetBlox";
										x.CurrentIdentity.MaxPlayerCount = 16;
										x.CurrentIdentity.Author = "NetBlox";
										x.CurrentRoot.Name = x.CurrentIdentity.PlaceName;
										x.CurrentRoot.Load(words[1]);
									}
									catch (Exception ex)
									{
										Console.WriteLine("Could not load the place: " + ex.Message);
									}
									break;
								case "killall":
									TaskScheduler.RunningJobs.Clear();
									break;
								case "jobs":
									int i = 0;
									Console.WriteLine("#) {0,-13} {1,-13} {2,-28} {3,-12}", "Name", "Type", "Paused until", "Is joined to job");
									TaskScheduler.RunningJobs.ForEach(x =>
									{
										Console.WriteLine("{0}) {1,-13} {2,-13} {3,-28} {4,-12}", i, x.Name, x.Type, x.JoinedUntil, x.JoinedTo != null);
										i++;
									});
									break;
								case "lua":
									TaskScheduler.ScheduleScript(x, cmd[4..], 8, null);
									break;
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine("Could not execute requested command: " + ex.Message);
						}
					}
				});
			});
			g.MainManager = true;
			AppManager.SetRenderTarget(g);
			AppManager.Start();
		}
	}
}
