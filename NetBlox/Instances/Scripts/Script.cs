using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
    public class Script : BaseScript
    {
        public override void Process()
        {
            if (!HadExecuted && AppManager.IsServer && Enabled)
            {
                LuaRuntime.RunScript(Source, true, this, 2, false);
                HadExecuted = true;
            }
        }
        [Lua]
        public override bool IsA(string classname)
        {
            if (nameof(Script) == classname) return true;
            return base.IsA(classname);
        }
    }
}
