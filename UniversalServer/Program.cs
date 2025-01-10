using MoonSharp.Interpreter;
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

				x.AllowReplication = true;
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
								case "test":
									if (int.TryParse(words[1], out int idx))
									{
										switch (idx)
										{
											case 0:
												Console.WriteLine("Beginning Animator test...");
												Workspace workspace = x.CurrentRoot.GetService<Workspace>();
												Part p0 = new(x)
												{
													Position = new System.Numerics.Vector3(0, 10, 0),
													Size = new System.Numerics.Vector3(4, 1, 2),
													Parent = workspace
												};
												Part p1 = new(x)
												{
													Position = new System.Numerics.Vector3(12, 10, 0),
													Size = new System.Numerics.Vector3(4, 1, 2),
													Parent = workspace
												};
												Animation anim = new(x)
												{
													Parent = workspace,
												};
												x.NetworkManager.AddReplication(p0, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_NEWINST);
												x.NetworkManager.AddReplication(p1, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_NEWINST);
												x.NetworkManager.AddReplication(anim, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_NEWINST);
												break;
											default:
												Console.WriteLine("No test is associated with " + idx);
												break;
										}
									}
									else
									{
										Console.WriteLine("Please type a number to run associated test!");
									}
									break;
								case "killall":
									TaskScheduler.RunningJobs.Clear();
									break;
								case "jobs":
									int i = 0;
									Console.WriteLine("#) {0,-13} {1,-28} {2,-12}", "Type", "Paused until", "Is joined to job");
									TaskScheduler.RunningJobs.ForEach(x =>
									{
										Console.WriteLine("{0}) {1,-13} {2,-28} {3,-12}", i, x.Type, x.JobTimingContext.JoinedUntil, x.JobTimingContext.JoinedTo != null);
										i++;
									});
									break;
								case "lua":
									TaskScheduler.ScheduleScript(x, cmd[4..], 8, null);
									break;
								case "luai":
									Table t = new(x.MainEnvironment);
									t.MetaTable = new Table(x.MainEnvironment);
									t.MetaTable["__index"] = x.MainEnvironment.Globals;
									while (true)
									{
										try
										{
											Console.ForegroundColor = ConsoleColor.Green;
											Console.Write("(lua interactive) >> ");
											Console.ResetColor();
											string code = Console.ReadLine() ?? "";

											if (code.Trim() == "exit")
												break;

											DynValue dynv = x.MainEnvironment.LoadString(code, t);

											x.MainEnvironment.Call(dynv);
										}
										catch (SyntaxErrorException see)
										{
											Console.ForegroundColor = ConsoleColor.Red;
											Console.WriteLine("Could not compile Lua: " + see.Message);
										}
										catch (Exception see)
										{
											Console.ForegroundColor = ConsoleColor.Red;
											Console.WriteLine("Runtime error from Lua: " + see.Message);
										}
									}
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
