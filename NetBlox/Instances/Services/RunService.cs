using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	[Service]
	public class RunService : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Heartbeat { get; init; }
		[Lua([Security.Capability.None])]
		public LuaSignal PostSimulation { get; init; }
		[Lua([Security.Capability.None])]
		public LuaSignal PreRender { get; init; }
		[Lua([Security.Capability.None])]
		public LuaSignal PreSimulation { get; init; }
		[Lua([Security.Capability.None])]
		public LuaSignal RenderStepped { get; init; }
		public DateTime LastTimeStartedRunning = DateTime.MinValue;

		public RunService(GameManager gm) : base(gm) 
		{
			Name = "Run Service";
			Heartbeat = new LuaSignal(gm);
			PostSimulation = new LuaSignal(gm);
			PreRender = new LuaSignal(gm);
			PreSimulation = new LuaSignal(gm);
			RenderStepped = new LuaSignal(gm);
		}

		[Lua([Security.Capability.CoreSecurity])]
		public void Pause() => GameManager.IsRunning = false;
		[Lua([Security.Capability.CoreSecurity])]
		public void Run() 
		{
			LastTimeStartedRunning = DateTime.UtcNow;
			GameManager.IsRunning = true; 
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Stop() => GameManager.Shutdown();
		[Lua([Security.Capability.None])]
		public bool IsClient() => GameManager.NetworkManager.IsClient;
		[Lua([Security.Capability.None])]
		public bool IsServer() => GameManager.NetworkManager.IsServer;
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RunService) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
			Heartbeat.Fire(DynValue.NewNumber(TaskScheduler.LastCycleTime.TotalSeconds));
		}
	}
}
