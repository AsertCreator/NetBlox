using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	public class Function
	{
		public bool IsNative;
		public Machine Machine;
		public Func<CallContext, RuntimeObject>? Callback;
		public int ArgumentCount = 0;
		private Instruction[] bytecode = [];
		private Dictionary<int, int> lines = [];
		private string source = "";

		public Function(Machine mc, string code)
		{
			Machine = mc;
			source = code;
			LoadCode(code, mc.AddDebugInformation);
		}
		public Function(Machine mc, Instruction[] code)
		{
			Machine = mc;
			bytecode = code;
		}
		private void LoadCode(string code, bool dbg)
		{
			code = code.Replace("\r\n", " ");
			code = code.Replace("\n", " ");

		}
		public RuntimeObject Invoke(CallContext cc)
		{
			for (int i = 0; i < bytecode.Length; i++)
			{
				var ins = bytecode[i];
				switch (ins.Opcode)
				{
					case Opcode.Nop: break;
					case Opcode.Add:
					{
						if (cc.Stack.Count < 2)
							throw new ScriptException("wrong flow control");
						var v0 = cc.Stack.Pop();
						var v1 = cc.Stack.Pop();
						cc.Stack.Push(v0.AddTo(v1));
							break;
					}
					case Opcode.Sub:
					{
						if (cc.Stack.Count < 2)
							throw new ScriptException("wrong flow control");
						var v0 = cc.Stack.Pop();
						var v1 = cc.Stack.Pop();
						cc.Stack.Push(v0.SubTo(v1));
							break;
					}
					case Opcode.Mul:
					{
						if (cc.Stack.Count < 2)
							throw new ScriptException("wrong flow control");
						var v0 = cc.Stack.Pop();
						var v1 = cc.Stack.Pop();
						cc.Stack.Push(v0.MulTo(v1));
							break;
					}
					case Opcode.Div:
					{
						if (cc.Stack.Count < 2)
							throw new ScriptException("wrong flow control");
						var v0 = cc.Stack.Pop();
						var v1 = cc.Stack.Pop();
						cc.Stack.Push(v0.DivTo(v1));
							break;
					}
					case Opcode.Ret:
						return cc.PopOrNil();
					case Opcode.Retn: //just discard everything lol
						return RuntimeObject.GetNil();
					case Opcode.Call:
						var obj = cc.PopOrNil();
						var fun = obj.EnsureFunction();
						cc.Stack.Push(fun.Invoke(cc.PopAtLeast((int)ins.Arg0)));
						break;
					case Opcode.Push1:
						cc.Stack.Push(RuntimeObject.FromInt(Machine, 1));
						break;
					case Opcode.Push2:
						cc.Stack.Push(RuntimeObject.FromInt(Machine, 2));
						break;
					case Opcode.Push3:
						cc.Stack.Push(RuntimeObject.FromInt(Machine, 3));
						break;
					case Opcode.Push4:
						cc.Stack.Push(RuntimeObject.FromInt(Machine, 4));
						break;
					case Opcode.PushNum:
						cc.Stack.Push(RuntimeObject.FromDouble(Machine, ins.Arg0));
						break;
					case Opcode.PushStr:
						cc.Stack.Push(RuntimeObject.FromString(Machine, ins.Arg1));
						break;
					case Opcode.PushTrue:
						cc.Stack.Push(RuntimeObject.FromBool(Machine, true));
						break;
					case Opcode.PushFalse:
						cc.Stack.Push(RuntimeObject.FromBool(Machine, false));
						break;
					case Opcode.PushNil:
						cc.Stack.Push(RuntimeObject.GetNil());
						break;
					case Opcode.NewTable:
						cc.Stack.Push(RuntimeObject.NewTable(Machine));
						break;
					case Opcode.GetGlobal:
						cc.Stack.Push(Machine.DefaultGlobal[cc.PopOrNil()]);
						break;
					case Opcode.SetGlobal:
						break;
					case Opcode.GetVariable:
						break;
					case Opcode.SetVariable:
						break;
					case Opcode.GetField:
						break;
					case Opcode.SetField:
						break;
					default:
						throw new ScriptException("invalid opcode");
				}
			}
			return cc.PopOrNil();
		}
		public RuntimeObject Invoke(params RuntimeObject[] ros) => Invoke(new CallContext(ros));
	}
}
