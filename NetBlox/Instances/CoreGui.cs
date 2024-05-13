using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System.Security.Cryptography;

namespace NetBlox.Instances
{
	[Creatable]
	[NotReplicated]
	public class CoreGui : Instance
	{
		private DynValue? showTeleportGui;
		private DynValue? hideTeleportGui;

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(CoreGui) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void SetShowTeleportGuiCallback(DynValue dv) => showTeleportGui = dv;
		[Lua([Security.Capability.CoreSecurity])]
		public void SetHideTeleportGuiCallback(DynValue dv) => hideTeleportGui = dv;
		[Lua([Security.Capability.CoreSecurity])]
		public void ShowTeleportGui(string placename, string authorname, int pid, int uid)
		{
			if (showTeleportGui != null && showTeleportGui.Type == DataType.Function)
			{
				LuaRuntime.ReportedExecute(() =>
				{
					showTeleportGui.Function.Call(placename, authorname, pid, uid);
				}, false);
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void HideTeleportGui()
		{
			if (hideTeleportGui != null && hideTeleportGui.Type == DataType.Function)
			{
				LuaRuntime.ReportedExecute(() =>
				{
					hideTeleportGui.Function.Call();
				}, false);
			}
		}
	}
}
