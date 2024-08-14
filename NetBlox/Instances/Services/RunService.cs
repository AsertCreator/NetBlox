using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances.Services
{
	public class RunService : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Heartbeat { get; init; } = new();
		public DateTime LastTimeStartedRunning = DateTime.MinValue;

		public RunService(GameManager gm) : base(gm) 
		{
			Name = "Run Service";
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
