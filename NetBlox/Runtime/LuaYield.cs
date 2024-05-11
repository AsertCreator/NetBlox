using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
    public class LuaYield<T>
    {
        public bool HasResult;
        public T? Result;
    }
}
