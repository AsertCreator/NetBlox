using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetBlox.Runtime
{
	public class RuntimeObject
	{
		public RuntimeType Type { get; init; }
		public object? Value { get => val; set => val = value; }
		public Machine Machine { get; init; }
		public bool IsNil => Type == RuntimeType.Nil;
		private object? val;

		public RuntimeObject(Machine m) => Machine = m;
		public RuntimeObject(Machine m, RuntimeType rt, object val)
		{
			Machine = m;
			Type = rt;
			Value = val;
		}
		public static RuntimeObject NewTable(Machine m) => new(m, RuntimeType.Table, new Table());
		public static RuntimeObject GetNil() => new(null!, RuntimeType.Nil, null!);
		public static RuntimeObject FromString(Machine m, string s) => new(m, RuntimeType.String, s);
		public static RuntimeObject FromDouble(Machine m, double s) => new(m, RuntimeType.Number, s);
		public static RuntimeObject FromInt(Machine m, int s) => new(m, RuntimeType.Number, (double)s);
		public static RuntimeObject FromBool(Machine m, bool s) => new(m, RuntimeType.Boolean, s);
		public unsafe override string ToString() => Type switch
		{
			RuntimeType.Nil => "nil",
			RuntimeType.Number => ((double)Value).ToString(),
			RuntimeType.Boolean => ((bool)Value ? "true" : "false"),
			RuntimeType.String => (string)Value ?? "",
			RuntimeType.Function => "function: " + new nint(Unsafe.AsPointer(ref val)).ToInt64().ToString("X16"),
			RuntimeType.Table => "table: " + new nint(Unsafe.AsPointer(ref val)).ToInt64().ToString("X16"),
			_ => "",
		};
		public Function EnsureFunction()
		{
			if (Type != RuntimeType.Function && (Value ?? throw new ScriptException("expected function, got nil")) is Function)
				throw new ScriptException("expected function, got " + Type.ToString().ToLower());
			return (Value as Function) ?? throw new ScriptException("corrupted runtime object");
		}
		public RuntimeObject AddTo(RuntimeObject b)
		{
			RuntimeObject a = this;
			if (a.Type == RuntimeType.Number && b.Type == RuntimeType.Number)
				return FromDouble(Machine, (double)a.Value + (double)b.Value);
			if (a.Type == RuntimeType.String && b.Type == RuntimeType.String)
				return FromString(Machine, (string)a.Value + (string)b.Value);
			if (a.Type != b.Type)
				throw new ScriptException("cannot perform artihmetics on two objects of different types");
			throw new ScriptException("cannot perform artihmetics on two objects, because types do not support it");
		}
		public RuntimeObject SubTo(RuntimeObject b)
		{
			RuntimeObject a = this;
			if (a.Type == RuntimeType.Number && b.Type == RuntimeType.Number)
				return FromDouble(Machine, (double)a.Value - (double)b.Value);
			if (a.Type != b.Type)
				throw new ScriptException("cannot perform artihmetics on two objects of different types");
			throw new ScriptException("cannot perform artihmetics on two objects, because types do not support it");
		}
		public RuntimeObject MulTo(RuntimeObject b)
		{
			RuntimeObject a = this;
			if (a.Type == RuntimeType.Number && b.Type == RuntimeType.Number)
				return FromDouble(Machine, (double)a.Value * (double)b.Value);
			if (a.Type != b.Type)
				throw new ScriptException("cannot perform artihmetics on two objects of different types");
			throw new ScriptException("cannot perform artihmetics on two objects, because types do not support it");
		}
		public RuntimeObject DivTo(RuntimeObject b)
		{
			RuntimeObject a = this;
			if (a.Type == RuntimeType.Number && b.Type == RuntimeType.Number)
				return FromDouble(Machine, (double)a.Value / (double)b.Value);
			if (a.Type != b.Type)
				throw new ScriptException("cannot perform artihmetics on two objects of different types");
			throw new ScriptException("cannot perform artihmetics on two objects, because types do not support it");
		}
	}
}
