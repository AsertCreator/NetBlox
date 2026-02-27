using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Network;
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

			Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath);

			TaskScheduler.ScheduleNamedJob("ServerBootstrap", JobType.Miscellaneous, _ =>
			{
				var bootstrapRendering = AppManager.GetFastFlag("FFlagServerBootstrapRendering", false);

				var g = AppManager.CreateGame(new()
				{
					AsServer = true,
					DoNotRenderAtAll = !bootstrapRendering,
					SkipWindowCreation = !bootstrapRendering,
					GameName = "NetBlox Server"
				}, args, (x) =>
				{
					x.PhysicsManager.DisablePhysics = true;

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

					x.PhysicsManager.SpringUpPhysics();
					x.PauseReplication = false;

					x.NetworkManager.StartServerNonBlocking();

					var commandlinethread = new Thread(EnterCommandLineLoop);
					commandlinethread.Start(x);
				});
				g.MainManager = true;
				AppManager.SetRenderTarget(g);
				return JobResult.CompletedSuccess;
			});

			AppManager.Start();
		}
		private static void EnterCommandLineLoop(object servergameobject)
		{
			GameManager servergame = servergameobject as GameManager;

			Console.WriteLine("NetBlox Server commmand line:");

			while (!servergame.ShuttingDown)
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
								servergame.CurrentRoot.Clear();
								servergame.CurrentIdentity.PlaceName = "Place downloaded from Web";
								servergame.CurrentIdentity.UniverseName = "NetBlox";
								servergame.CurrentIdentity.MaxPlayerCount = 16;
								servergame.CurrentIdentity.Author = "NetBlox";
								servergame.CurrentRoot.Name = servergame.CurrentIdentity.PlaceName;
								servergame.CurrentRoot.Load(words[1]);
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
										Workspace workspace = servergame.CurrentRoot.GetService<Workspace>();
										Part p0 = new(servergame)
										{
											Position = new System.Numerics.Vector3(0, 10, 0),
											Size = new System.Numerics.Vector3(4, 1, 2),
											Parent = workspace
										};
										Part p1 = new(servergame)
										{
											Position = new System.Numerics.Vector3(12, 10, 0),
											Size = new System.Numerics.Vector3(4, 1, 2),
											Parent = workspace
										};
										Animation anim = new(servergame)
										{
											Parent = workspace,
										};
										servergame.NetworkManager.AddReplication(p0, Replication.REPM_TOALL, Replication.REPW_NEWINST);
										servergame.NetworkManager.AddReplication(p1, Replication.REPM_TOALL, Replication.REPW_NEWINST);
										servergame.NetworkManager.AddReplication(anim, Replication.REPM_TOALL, Replication.REPW_NEWINST);
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
							TaskScheduler.ScheduleScript(servergame, cmd[4..], 8, null);
							break;
						case "luai":
							Table t = new(servergame.MainEnvironment);
							t.MetaTable = new Table(servergame.MainEnvironment);
							t.MetaTable["__index"] = servergame.MainEnvironment.Globals;
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

									DynValue dynv = servergame.MainEnvironment.LoadString(code, t);

									servergame.MainEnvironment.Call(dynv);
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
		}
	}
}
