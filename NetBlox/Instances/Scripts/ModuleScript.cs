using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances.Scripts
{
	[Creatable]
	public class ModuleScript : BaseScript
	{
		public bool DoneModulating;
		public DynValue? DynValue;
		public ModuleScript(GameManager ins) : base(ins) { }

		public override void Process()
		{
			// we dont do THAT
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ModuleScript) == classname) return true;
			return base.IsA(classname);
		}
	}
}
