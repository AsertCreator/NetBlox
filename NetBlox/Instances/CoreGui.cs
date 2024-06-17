using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Security.Cryptography;

namespace NetBlox.Instances
{
	[NotReplicated]
	public class CoreGui : Instance
	{
		private DynValue? showTeleportGui;
		private DynValue? hideTeleportGui;

		public CoreGui(GameManager ins) : base(ins) { }

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
		public void Notify(string title, string message)
		{
			try
			{
				Root.GetService<StarterGui>().SetCore("SendNotification", DynValue.NewTable(new Table(Root.MainEnv)
				{
					["Title"] = title,
					["Text"] = message,
				}));
			}
			catch { } // we tried
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
		[Lua([Security.Capability.CoreSecurity])]
		public void TakeScreenshot()
		{
			string path = AppManager.LibraryFolder + "/" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".png";
			Raylib.TakeScreenshot(path);
			Notify("Screenshot taken!", "Check the screenshort folder");
		}
	}
}
