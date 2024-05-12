using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.GUIs
{
    public class GuiObject : Instance
    {
        [Lua([Security.Capability.None])]
        public override bool IsA(string classname)
        {
            if (nameof(GuiObject) == classname) return true;
            return base.IsA(classname);
        }
        public override void RenderUI()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].RenderUI();
            }
        }
    }
}
