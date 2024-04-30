using MoonSharp.Interpreter;
using NetBlox.Runtime;

namespace NetBlox.Instances
{
	[Creatable]
    [NotReplicated]
	public class CoreGui : Instance
	{
		private DynValue? showTeleportGui;
        private DynValue? hideTeleportGui;

        [Lua]
		public override bool IsA(string classname)
		{
			if (nameof(CoreGui) == classname) return true;
			return base.IsA(classname);
        }
        [Lua]
        public void SetShowTeleportGuiCallback(DynValue dv) => showTeleportGui = dv;
        [Lua]
        public void SetHideTeleportGuiCallback(DynValue dv) => hideTeleportGui = dv;
        [Lua]
		public void ShowTeleportGui(string placename, string authorname, int pid, int uid)
		{
			if (showTeleportGui != null && showTeleportGui.Type == DataType.Function)
				showTeleportGui.Function.Call(placename, authorname, pid, uid);
        }
        [Lua]
        public void HideTeleportGui()
        {
            if (hideTeleportGui != null && hideTeleportGui.Type == DataType.Function)
				hideTeleportGui.Function.Call();
        }
    }
}
