
//
// This part taken from KopiLua - https://github.com/NLua/KopiLua
//
// =========================================================================================================
//
// Kopi Lua License
// ----------------
// MIT License for KopiLua
// Copyright (c) 2012 LoDC
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ===============================================================================
// Lua License
// -----------
// Lua is licensed under the terms of the MIT license reproduced below.
// This means that Lua is free software and can be used for both academic
// and commercial purposes at absolutely no cost.
// For details and rationale, see http://www.lua.org/license.html .
// ===============================================================================
// Copyright (C) 1994-2008 Lua.org, PUC-Rio.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;

namespace MoonSharp.Interpreter.Interop.LuaStateInterop
{
	public class CharPtr
	{
		public char[] chars;
		public int index;

		public char this[int offset]
		{
			get => chars[index + offset];
			set => chars[index + offset] = value;
		}

		public char this[uint offset]
		{
			get => chars[index + offset];
			set => chars[index + offset] = value;
		}
		public char this[long offset]
		{
			get => chars[index + (int)offset];
			set => chars[index + (int)offset] = value;
		}

		public static implicit operator CharPtr(string str) => new CharPtr(str);
		public static implicit operator CharPtr(char[] chars) => new CharPtr(chars);
		public static implicit operator CharPtr(byte[] bytes) => new CharPtr(bytes);

		public CharPtr()
		{
			this.chars = null;
			this.index = 0;
		}

		public CharPtr(string str)
		{
			this.chars = (str + '\0').ToCharArray();
			this.index = 0;
		}

		public CharPtr(CharPtr ptr)
		{
			this.chars = ptr.chars;
			this.index = ptr.index;
		}

		public CharPtr(CharPtr ptr, int index)
		{
			this.chars = ptr.chars;
			this.index = index;
		}

		public CharPtr(char[] chars)
		{
			this.chars = chars;
			this.index = 0;
		}

		public CharPtr(char[] chars, int index)
		{
			this.chars = chars;
			this.index = index;
		}

		public CharPtr(byte[] bytes)
		{
			this.chars = new char[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				this.chars[i] = (char)bytes[i];
			}

			this.index = 0;
		}

		public CharPtr(IntPtr ptr)
		{
			this.chars = new char[0];
			this.index = 0;
		}

		public static CharPtr operator +(CharPtr ptr, int offset) => new CharPtr(ptr.chars, ptr.index + offset);
		public static CharPtr operator -(CharPtr ptr, int offset) => new CharPtr(ptr.chars, ptr.index - offset);
		public static CharPtr operator +(CharPtr ptr, uint offset) => new CharPtr(ptr.chars, ptr.index + (int)offset);
		public static CharPtr operator -(CharPtr ptr, uint offset) => new CharPtr(ptr.chars, ptr.index - (int)offset);

		public void inc() => this.index++;
		public void dec() => this.index--;
		public CharPtr next() => new CharPtr(this.chars, this.index + 1);
		public CharPtr prev() => new CharPtr(this.chars, this.index - 1);
		public CharPtr add(int ofs) => new CharPtr(this.chars, this.index + ofs);
		public CharPtr sub(int ofs) => new CharPtr(this.chars, this.index - ofs);

		public static bool operator ==(CharPtr ptr, char ch) => ptr[0] == ch;
		public static bool operator ==(char ch, CharPtr ptr) => ptr[0] == ch;
		public static bool operator !=(CharPtr ptr, char ch) => ptr[0] != ch;
		public static bool operator !=(char ch, CharPtr ptr) => ptr[0] != ch;

		public static CharPtr operator +(CharPtr ptr1, CharPtr ptr2)
		{
			string result = "";
			for (int i = 0; ptr1[i] != '\0'; i++)
				result += ptr1[i];
			for (int i = 0; ptr2[i] != '\0'; i++)
				result += ptr2[i];
			return new CharPtr(result);
		}
		public static int operator -(CharPtr ptr1, CharPtr ptr2)
		{
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index - ptr2.index;
		}
		public static bool operator <(CharPtr ptr1, CharPtr ptr2)
		{
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index < ptr2.index;
		}
		public static bool operator <=(CharPtr ptr1, CharPtr ptr2)
		{
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index <= ptr2.index;
		}
		public static bool operator >(CharPtr ptr1, CharPtr ptr2)
		{
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index > ptr2.index;
		}
		public static bool operator >=(CharPtr ptr1, CharPtr ptr2)
		{
			Debug.Assert(ptr1.chars == ptr2.chars); return ptr1.index >= ptr2.index;
		}
		public static bool operator ==(CharPtr ptr1, CharPtr ptr2)
		{
			object o1 = ptr1;
			object o2 = ptr2;
			return ((o1 == null) && (o2 == null)) || (o1 != null && o2 != null && (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index));
		}
		public static bool operator !=(CharPtr ptr1, CharPtr ptr2) => !(ptr1 == ptr2);

		public override bool Equals(object o) => this == (o as CharPtr);

		public override int GetHashCode() => 0;
		public override string ToString()
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			for (int i = index; (i < chars.Length) && (chars[i] != '\0'); i++)
				result.Append(chars[i]);

			return result.ToString();
		}

		public string ToString(int length)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			for (int i = index; (i < chars.Length) && i < (length + index); i++)
				result.Append(chars[i]);
			return result.ToString();
		}
	}

}
