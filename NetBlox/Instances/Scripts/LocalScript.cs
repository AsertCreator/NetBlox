using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
	[Creatable]
	public class LocalScript : BaseScript
    {
        public override void Process()
        {
            if (!HadExecuted && NetworkManager.IsClient && Enabled) // we can only execute 
            {
                LuaRuntime.Execute(Source, 2, this, GameManager.CurrentRoot);
                HadExecuted = true;
            }
        }
        [Lua([Security.Capability.None])]
        public override bool IsA(string classname)
        {
            if (nameof(LocalScript) == classname) return true;
            return base.IsA(classname);
        }
    }
}
