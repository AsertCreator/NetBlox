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
		public RunService() 
		{
			Name = "Run Service";
		}
		[Lua]
		public void Pause() => GameManager.IsRunning = false;
		[Lua]
		public void Run() => GameManager.IsRunning = true;
		[Lua]
		public void Stop() => GameManager.MessageQueue.Enqueue(new Message() { Type = MessageType.Shutdown });
		[Lua]
		public bool IsClient() => GameManager.CurrentIdentity != null;
		[Lua]
		public bool IsServer() => GameManager.CurrentIdentity != null;
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(RunService) == classname) return true;
			return base.IsA(classname);
		}
	}
}
