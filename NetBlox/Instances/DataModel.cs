using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Diagnostics;
using System.Net.Http;

namespace NetBlox.Instances
{
	public class DataModel : ServiceProvider
	{
		public DataModel(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public bool IsLoaded()
		{
			return true;
		}
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
		public void Shutdown()
		{
			GameManager.Shutdown();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string HttpGet(string url) => File.ReadAllText(AppManager.ResolveUrlAsync(url, true).WaitAndGetResult());
		[Lua([Security.Capability.CoreSecurity])]
		public Instance[] GetObjects(string url) => [];
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
