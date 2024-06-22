using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances.Scripts
{
	public class BaseScript : LuaSourceContainer
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;
		public bool HadExecuted = false;

		public BaseScript(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(BaseScript) == classname) return true;
			return base.IsA(classname);
		}
		public override Instance Clone()
		{
			var inst = (BaseScript)base.Clone();
			inst.HadExecuted = false;
			return inst;
		}
	}
}
