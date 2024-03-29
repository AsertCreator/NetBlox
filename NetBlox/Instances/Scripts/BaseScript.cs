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
        [Lua]
        public bool Enabled { get; set; }
        protected bool HadExecuted = false;

        [Lua]
        public override bool IsA(string classname)
        {
            if (nameof(BaseScript) == classname) return true;
            return base.IsA(classname);
        }
    }
}
