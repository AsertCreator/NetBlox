using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances.Scripts
{
	public class ModuleScript : BaseScript
	{
		public override void Process()
		{
			// we dont do THAT
		}
		public DynValue Modulate()
		{
			throw new NotImplementedException();
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Script) == classname) return true;
			return base.IsA(classname);
		}
	}
}
