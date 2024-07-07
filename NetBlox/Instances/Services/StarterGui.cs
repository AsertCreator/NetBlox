using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class StarterGui : Instance
	{
		public StarterGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(StarterGui) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void RegisterSetCore(string name, DynValue func)
		{
			CoreGui cg = Root.GetService<CoreGui>();
			if (func.Type != DataType.Function) throw new Exception($"RegisterSetCore only accepts functions");
			cg.RegisteredSetCallbacks[name] = func;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void RegisterGetCore(string name, DynValue func)
		{
			CoreGui cg = Root.GetService<CoreGui>();
			if (func.Type != DataType.Function) throw new Exception($"RegisterGetCore only accepts functions");
			cg.RegisteredGetCallbacks[name] = func;
		}
		[Lua([Security.Capability.None])]
		public void SetCore(string name, DynValue dv)
		{
			CoreGui cg = Root.GetService<CoreGui>();
			if (cg.RegisteredSetCallbacks.ContainsKey(name))
				TaskScheduler.ScheduleScript(GameManager, cg.RegisteredSetCallbacks[name], 3, null, null, [dv]);
			else throw new Exception($"\"{name}\" has not been registered by CoreScripts");
		}
		[Lua([Security.Capability.None])]
		public DynValue GetCore(string name)
		{
			throw new Exception($"im not sure yielding works here");
		}
	}
}
