using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class ScriptContext : Instance
	{
		public ScriptContext(GameManager ins) : base(ins) 
		{
			Name = "Script Context";
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ScriptContext) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public void SetTimeout(double d) => LuaRuntime.ScriptExecutionTimeout = (int)(d * 1000);
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public DynValue Compile(string code)
		{
			var func = GameManager.MainEnvironment.LoadString(code);
			return func;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void AddCoreScriptLocal(string path, Instance parent)
		{
			CoreScript cs = new(GameManager);
			cs.Name = path;
			cs.Source = File.ReadAllText(AppManager.ResolveUrlAsync("rbxasset://scripts/" + cs.Name + ".lua", false).WaitAndGetResult());
			cs.Parent = parent;
		}
	}
}
