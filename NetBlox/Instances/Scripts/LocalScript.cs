using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
    public class LocalScript : BaseScript
    {
        public override void Process()
        {
            if (!HadExecuted && !AppManager.IsServer && Enabled)
            {
                LuaRuntime.RunScript(Source, true, this, 2, false);
                HadExecuted = true;
            }
        }
        [Lua]
        public override bool IsA(string classname)
        {
            if (nameof(LocalScript) == classname) return true;
            return base.IsA(classname);
        }
    }
}
