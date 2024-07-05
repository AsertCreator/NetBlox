using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Security.Cryptography;

namespace NetBlox.Instances
{
	[NotReplicated]
	public class CoreGui : Instance
	{
		public override Security.Capability[] RequiredCapabilities => [Security.Capability.CoreSecurity];

		[Lua([Security.Capability.CoreSecurity])]
		public LuaSignal OnTeleportStarts { get; init; } = new LuaSignal();
		[Lua([Security.Capability.CoreSecurity])]
		public LuaSignal OnTeleportEnds { get; init; } = new LuaSignal();

		public CoreGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(CoreGui) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void ShowTeleportGui(string placename, string authorname, int pid, int uid) => OnTeleportStarts.Fire(DynValue.NewString(placename), DynValue.NewString(authorname), DynValue.NewNumber(pid), DynValue.NewNumber(uid));
		[Lua([Security.Capability.CoreSecurity])]
		public void HideTeleportGui() => OnTeleportEnds.Fire();
		[Lua([Security.Capability.CoreSecurity])]
		public void Notify(string title, string message)
		{
			try
			{
				Root.GetService<StarterGui>().SetCore("SendNotification", DynValue.NewTable(new Table(GameManager.MainEnvironment)
				{
					["Title"] = title,
					["Text"] = message,
				}));
			}
			catch { } // we tried
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
