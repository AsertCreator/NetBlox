using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	public class ScriptException : Exception
	{
		public ScriptException() { }
		public ScriptException(string msg) : base(msg) { }
	}
}
