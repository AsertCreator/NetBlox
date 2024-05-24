using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBlox.Runtime
{
	public class Machine
	{
		public Table DefaultGlobal { get; set; } = new Table();
		public bool CompileToMSIL { get; set; } = false;
		public bool AddDebugInformation { get; set; } = false;
		public Function Compile(string code) => new(this, code);
	}
	public class CallContext
	{
		public RuntimeObject[] Arguments = [];
		public Stack<RuntimeObject> Stack = [];

		public CallContext(params RuntimeObject[] ros) => Arguments = ros;

		public RuntimeObject PopOrNil()
		{
			if (Stack.Count > 0) return Stack.Pop();
			return RuntimeObject.GetNil();
		}
		public RuntimeObject[] PopAtLeast(int n)
		{
			if (Stack.Count < n) throw new ScriptException("expected at least " + n + " items on stack");
			var ls = new List<RuntimeObject>();
			for (int i = 0; i < n; i++)
				ls.Add(PopOrNil());
			return ls.ToArray();
		}
	}
	public struct Instruction
	{
		public Opcode Opcode;
		public double Arg0;
		public string Arg1;
	}
	public enum Opcode
	{
		Nop, Add, Sub, Mul, Div, Ret, Retn, Call,
		Push1, Push2, Push3, Push4, PushNum, PushStr, PushTrue, PushFalse, PushNil, NewTable, 
		GetGlobal, SetGlobal, GetVariable, SetVariable, GetField, SetField
	}
}
