using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
    public class LocalScript : BaseScript
    {
        public override void Process()
        {
            if (!HadExecuted && !GameManager.IsServer && Enabled)
            {
                LuaRuntime.Execute(Source, 2, this, GameManager.CurrentRoot);
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
