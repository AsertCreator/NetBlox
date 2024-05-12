using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
    [Creatable]
    public class ScreenGui : Instance
    {
        [Lua([Security.Capability.None])]
        public override bool IsA(string classname)
        {
            if (nameof(ScreenGui) == classname) return true;
            return base.IsA(classname);
        }
    }
}
