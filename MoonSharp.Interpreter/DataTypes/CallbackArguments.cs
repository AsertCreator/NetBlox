using MoonSharp.Interpreter.DataStructs;
using System.Collections.Generic;

namespace MoonSharp.Interpreter.DataTypes
{
	/// <summary>
	/// This class is a container for arguments received by a CallbackFunction
	/// </summary>
	public class CallbackArguments
	{
		private readonly IList<DynValue> m_Args;
		private readonly bool m_LastIsTuple = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallbackArguments" /> class.
		/// </summary>
		/// <param name="args">The arguments.</param>
		/// <param name="isMethodCall">if set to <c>true</c> [is method call].</param>
		public CallbackArguments(IList<DynValue> args, bool isMethodCall)
		{
			m_Args = args;

			if (m_Args.Count > 0)
			{
				var last = m_Args[m_Args.Count - 1];

				if (last.Type == DataType.Tuple)
				{
					Count = last.Tuple.Length - 1 + m_Args.Count;
					m_LastIsTuple = true;
				}
				else
				{
					Count = last.Type == DataType.Void ? m_Args.Count - 1 : m_Args.Count;
				}
			}
			else
			{
				Count = 0;
			}

			IsMethodCall = isMethodCall;
		}

		/// <summary>
		/// Gets the count of arguments
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this is a method call.
		/// </summary>
		public bool IsMethodCall { get; private set; }


		/// <summary>
		/// Gets the <see cref="DynValue"/> at the specified index, or Void if not found 
		/// </summary>
		public DynValue this[int index] => RawGet(index, true) ?? DynValue.Void;

		/// <summary>
		/// Gets the <see cref="DynValue" /> at the specified index, or null.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="translateVoids">if set to <c>true</c> all voids are translated to nils.</param>
		/// <returns></returns>
		public DynValue RawGet(int index, bool translateVoids)
		{
			DynValue v;

			if (index >= Count)
				return null;

			v = !m_LastIsTuple || index < m_Args.Count - 1 ? m_Args[index] : m_Args[m_Args.Count - 1].Tuple[index - (m_Args.Count - 1)];

			if (v.Type == DataType.Tuple)
			{
				v = v.Tuple.Length > 0 ? v.Tuple[0] : DynValue.Nil;
			}

			if (translateVoids && v.Type == DataType.Void)
			{
				v = DynValue.Nil;
			}

			return v;
		}


		/// <summary>
		/// Converts the arguments to an array
		/// </summary>
		/// <param name="skip">The number of elements to skip (default= 0).</param>
		/// <returns></returns>
		public DynValue[] GetArray(int skip = 0)
		{
			if (skip >= Count)
				return new DynValue[0];

			DynValue[] vals = new DynValue[Count - skip];

			for (int i = skip; i < Count; i++)
				vals[i - skip] = this[i];

			return vals;
		}

		/// <summary>
		/// Gets the specified argument as as an argument of the specified type. If not possible,
		/// an exception is raised.
		/// </summary>
		/// <param name="argNum">The argument number.</param>
		/// <param name="funcName">Name of the function.</param>
		/// <param name="type">The type desired.</param>
		/// <param name="allowNil">if set to <c>true</c> nil values are allowed.</param>
		/// <returns></returns>
		public DynValue AsType(int argNum, string funcName, DataType type, bool allowNil = false) => this[argNum].CheckType(funcName, type, argNum, allowNil ? TypeValidationFlags.AllowNil | TypeValidationFlags.AutoConvert : TypeValidationFlags.AutoConvert);

		/// <summary>
		/// Gets the specified argument as as an argument of the specified user data type. If not possible,
		/// an exception is raised.
		/// </summary>
		/// <typeparam name="T">The desired userdata type</typeparam>
		/// <param name="argNum">The argument number.</param>
		/// <param name="funcName">Name of the function.</param>
		/// <param name="allowNil">if set to <c>true</c> nil values are allowed.</param>
		/// <returns></returns>
		public T AsUserData<T>(int argNum, string funcName, bool allowNil = false) => this[argNum].CheckUserDataType<T>(funcName, argNum, allowNil ? TypeValidationFlags.AllowNil : TypeValidationFlags.None);

		/// <summary>
		/// Gets the specified argument as an integer
		/// </summary>
		/// <param name="argNum">The argument number.</param>
		/// <param name="funcName">Name of the function.</param>
		/// <returns></returns>
		public int AsInt(int argNum, string funcName)
		{
			DynValue v = AsType(argNum, funcName, DataType.Number, false);
			double d = v.Number;
			return (int)d;
		}

		/// <summary>
		/// Gets the specified argument as a long integer
		/// </summary>
		/// <param name="argNum">The argument number.</param>
		/// <param name="funcName">Name of the function.</param>
		/// <returns></returns>
		public long AsLong(int argNum, string funcName)
		{
			DynValue v = AsType(argNum, funcName, DataType.Number, false);
			double d = v.Number;
			return (long)d;
		}


		/// <summary>
		/// Gets the specified argument as a string, calling the __tostring metamethod if needed, in a NON
		/// yield-compatible way.
		/// </summary>
		/// <param name="executionContext">The execution context.</param>
		/// <param name="argNum">The argument number.</param>
		/// <param name="funcName">Name of the function.</param>
		/// <returns></returns>
		/// <exception cref="ScriptRuntimeException">'tostring' must return a string to '{0}'</exception>
		public string AsStringUsingMeta(ScriptExecutionContext executionContext, int argNum, string funcName)
		{
			if (this[argNum].Type == DataType.Table && this[argNum].Table.MetaTable != null &&
				this[argNum].Table.MetaTable.RawGet("__tostring") != null)
			{
				var v = executionContext.GetScript().Call(this[argNum].Table.MetaTable.RawGet("__tostring"), this[argNum]);

				return v.Type != DataType.String
					? throw new ScriptRuntimeException("'tostring' must return a string to '{0}'", funcName)
					: v.ToPrintString();
			}
			else
			{
				return this[argNum].ToPrintString();
			}
		}


		/// <summary>
		/// Returns a copy of CallbackArguments where the first ("self") argument is skipped if this was a method call,
		/// otherwise returns itself.
		/// </summary>
		/// <returns></returns>
		public CallbackArguments SkipMethodCall()
		{
			if (IsMethodCall)
			{
				Slice<DynValue> slice = new Slice<DynValue>(m_Args, 1, m_Args.Count - 1, false);
				return new CallbackArguments(slice, false);
			}
			else return this;
		}
	}
}
