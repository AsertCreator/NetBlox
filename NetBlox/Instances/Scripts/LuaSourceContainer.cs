using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
    public class LuaSourceContainer : Instance
    {
        [Lua]
        public string Source { get; set; } = string.Empty;

        [Lua]
        public override bool IsA(string classname)
        {
            if (nameof(LuaSourceContainer) == classname) return true;
            return base.IsA(classname);
        }
    }
}
