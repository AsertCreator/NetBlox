using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime;

namespace NetBlox.Instances
{
	public class DataModel : ServiceProvider
	{
		[Lua([Security.Capability.None])]
		public long CreatorId => GameManager.CurrentIdentity.Author.GetHashCode(); // worky around
		[Lua([Security.Capability.None])]
		public long GameId => (long)GameManager.CurrentIdentity.UniverseID;
		[Lua([Security.Capability.None])]
		public long PlaceId => (long)GameManager.CurrentIdentity.PlaceID;
		[Lua([Security.Capability.None])]
		public int PlaceVersion => 0;

		public DataModel(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public bool IsLoaded() => GameManager.NetworkManager.IsLoaded;
		[Lua([Security.Capability.CoreSecurity])]
		public bool GetFastFlag(string fflag) => AppManager.FastFlags.TryGetValue(fflag, out var flag) ? flag : throw new Exception("No such FastFlag defined!");
		[Lua([Security.Capability.CoreSecurity])]
		public int GetFastInt(string fflag) => AppManager.FastInts.TryGetValue(fflag, out var flag) ? flag : throw new Exception("No such FastInt defined!");
		[Lua([Security.Capability.CoreSecurity])]
		public string GetFastString(string fflag) => AppManager.FastStrings.TryGetValue(fflag, out var flag) ? flag : throw new Exception("No such FastString defined!");
		[Lua([Security.Capability.CoreSecurity])]
		public void Clear()
		{
			LogManager.LogInfo("Clearing DataModel...");
			GetService<ReplicatedFirst>().Destroy();
			GetService<Workspace>().Destroy();
			GetService<ReplicatedStorage>().Destroy();
			GetService<Lighting>().Destroy();
			GetService<Players>().Destroy();
			GetService<StarterGui>().Destroy();
			GetService<StarterPack>().Destroy();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void BindToClose(DynValue dv)
		{
			if (dv.Type != DataType.Function)
				throw new Exception("expected function to be passed to BindToClose");
			GameManager.ShutdownEvent += (x, y) =>
			{
				CancellationTokenSource cts = new();
				var task = Task.Run(() =>
				{
					dv.Function.Call(dv.Type);
				});
				if (!task.Wait(30000))
				{
					LogManager.LogWarn("One of BindToClose's function is taking too long, shutting down anyway...");
				}
			};
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Shutdown()
		{
			GameManager.Shutdown();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void OpenScreenshotFolder() => GetService<PlatformService>().OpenBrowserWindow(AppManager.LibraryFolder);
		[Lua([Security.Capability.CoreSecurity])]
		public string HttpGet(string url) => File.ReadAllText(AppManager.ResolveUrlAsync(url, true).WaitAndGetResult());
		[Lua([Security.Capability.CoreSecurity])]
		public LuaYield HttpGetAsync(string url) 
		{
			var job = TaskScheduler.CurrentJob;
			job.TaskJoinedTo = Task.Run(async () =>
			{
				var path = await AppManager.ResolveUrlAsync(url, true);
				var data = File.ReadAllText(path);
				job.AssociatedObject4 = new DynValue[] { DynValue.NewString(data) };
			});
			return new();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public Instance[] GetObjects(string url)
		{
			Instance ins = new(GameManager); // temporary holder
			RbxlParser.Load(url, ins);
			Instance[] chlidren = ins.GetChildren();
			ins.ClearAllChildren();
			ins.Destroy();
			return chlidren;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Load(string url)
		{
			LogManager.LogInfo("Loading DataModel from URL " + url + "...");
			Clear();
			RbxlParser.Load(url, this);
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
