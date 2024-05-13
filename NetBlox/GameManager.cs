global using Color = Raylib_cs.Color;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;
using System.Runtime;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);
	public class GameManager
	{
		public List<Instance> AllInstances = [];
		public List<NetworkClient> AllClients = [];
		public Dictionary<char, Action> Verbs = [];
		public NetworkIdentity CurrentIdentity = new();
		public RenderManager? RenderManager;
		public NetworkManager? NetworkManager;
		public DataModel CurrentRoot = null!;
		public bool IsRunning = true;
		public bool ShuttingDown = false;
		public string ManagerName = "";
		public string? Username = "DevDevDev" + Random.Shared.Next(1000, 9999);
		public event EventHandler? ShutdownEvent;
		public event InstanceEventHandler? AddedInstance;
		public bool AllowReplication = false;

		public void InvokeAddedEvent(Instance inst)
		{
			if (AddedInstance != null && AllowReplication)
				AddedInstance(inst);
		}
		public void LoadAllCoreScripts()
		{
			string[] files = Directory.GetFiles(SharedData.ContentFolder + "scripts");
			for (int i = 0; i < files.Length; i++)
			{
				CoreScript cs = new(this);
				string cont = File.ReadAllText(files[i]);
				cs.Source = cont;
				cs.Name = Path.GetFileName(files[i]);
				cs.Parent = CurrentRoot.GetService<CoreGui>();
			}
		}
		public void Start(bool client, bool server, bool render, string[] args, Action<string, GameManager> servercallback)
		{
			ulong pid = ulong.MaxValue;
			string rbxlinit = "";
			LogManager.LogInfo("Initializing NetBlox...");

			NetworkManager = new(this, server, client);
			CurrentIdentity.Reset();

			// common thingies
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--fast-flag":
						{
							var key = args[++i];
							var bo = int.Parse(args[++i]) == 1;

							SharedData.FastFlags[key] = bo;
							LogManager.LogInfo($"Setting fast flag {key} to {bo}");
							break;
						}
					case "--fast-string":
						{
							var key = args[++i];
							var st = args[++i];

							SharedData.FastStrings[key] = st;
							LogManager.LogInfo($"Setting fast stirng {key} to \"{st}\"");
							break;
						}
					case "--fast-number":
						{
							var key = args[++i];
							var nu = int.Parse(args[++i]);

							SharedData.FastNumbers[key] = nu;
							LogManager.LogInfo($"Setting fast number {key} to {nu}");
							break;
						}
					case "--placeor":
						{
							CurrentIdentity.PlaceName = args[++i];
							break;
						}
					case "--univor":
						{
							CurrentIdentity.UniverseName = args[++i];
							break;
						}
					case "--maxplayers":
						{
							CurrentIdentity.MaxPlayerCount = uint.Parse(args[++i]);
							break;
						}
					case "--rbxl":
						{
							rbxlinit = args[++i];
							break;
						}
					default:
						{
							LogManager.LogError($"Unknown console argument: {args[i]}");
							break;
						}
				}
			}

			LogManager.LogInfo("Initializing verbs...");
			Verbs.Add(',', () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);

			LogManager.LogInfo("Initializing RenderManager...");
			RenderManager = new(this, render);

			LogManager.LogInfo("Initializing SerializationManager...");
			SerializationManager.Initialize();

			LogManager.LogInfo("Initializing internal scripts...");
			CurrentRoot = new DataModel(this);

			var rs = new RunService(this);
			var cg = new CoreGui(this);
			rs.Parent = CurrentRoot;
			cg.Parent = CurrentRoot;

			LuaRuntime.Setup(this, CurrentRoot, true);
			LoadAllCoreScripts();

			if (NetworkManager.IsClient)
			{
				CurrentRoot.GetService<CoreGui>().ShowTeleportGui("", "", -1, -1);
				servercallback(rbxlinit, this);
			}
			if (NetworkManager.IsServer)
			{
				AddedInstance += (x) =>
				{
					lock (NetworkManager.ToReplicate)
					{
						NetworkManager.ToReplicate.Enqueue(new()
						{
							What = x
						});
					}
				};
				servercallback(rbxlinit, this);
			}

			while (!ShuttingDown) ;
		}
		public void TeleportToPlace(ulong pid)
		{
			if (NetworkManager.IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			LogManager.LogInfo($"Teleporting to the place ({pid})...");
			// no actual servers as of now, so just hardcoded values
			string pname = "Testing Place";
			ulong pauth = 1;
			LogManager.LogInfo($"Place has name ({pname}) and author ({pauth})...");
			NetworkManager.ConnectToServer(null!);
		}
		public void Shutdown()
		{
			LogManager.LogInfo("Shutting down...");
			ShuttingDown = true;
			ShutdownEvent?.Invoke(new(), new());
		}
		public Instance? GetInstance(Guid id)
		{
			for (int i = 0; i < AllInstances.Count; i++)
			{
				if (AllInstances[i].UniqueID == id)
					return AllInstances[i];
			}
			return null;
		}
		public void ProcessInstance(Instance inst)
		{
			if (inst != null)
			{ // i was outsmarted
				inst.Process();

				var ch = inst.GetChildren();
				for (int i = 0; i < ch.Length; i++)
				{
					ProcessInstance(ch[i]);
				}
			}
		}
	}
}
