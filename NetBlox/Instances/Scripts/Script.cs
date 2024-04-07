using NetBlox.Runtime;

namespace NetBlox.Instances.Scripts
{
	[Creatable]
	public class Script : BaseScript
    {
        public override void Process()
        {
            if (!HadExecuted && NetworkManager.IsServer && Enabled)
			{
				LuaRuntime.Execute(Source, 2, this, GameManager.CurrentRoot);
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
