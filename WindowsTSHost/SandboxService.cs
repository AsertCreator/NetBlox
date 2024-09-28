using NetBlox.Common;
using NetBlox.Instances.GUIs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Text.Json;

namespace NetBlox.Instances.Services
{
	public partial class SandboxService
	{
		private SandboxTitleFrame? frame;
		private int preloadDummy = (new Func<int>(() =>
		{
			if (!Directory.Exists("sandboxes"))
				Directory.CreateDirectory("sandboxes");
			return 0;
		}))();
		public SandboxContext? CurrentContext;

		[Lua([Security.Capability.CoreSecurity])]
		public void StartTeamSandboxTitle()
		{
			LogManager.LogInfo("SandboxService::StartTeamSandboxTitle has been called");
			var cg = Root.GetService<CoreGui>();

			if (frame == null)
			{
				frame = new SandboxTitleFrame(GameManager);
				frame.Name = "TSHTitleFrame";
				frame.Position = new UDim2();
				frame.Size = new UDim2(1, 0, 1, 0);
				frame.Parent = cg.FindFirstChild("RobloxGui");
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void StartTeamSandbox(SandboxContext ctx)
		{
			LogManager.LogInfo("SandboxService::StartTeamSandbox has been called");
			if (CurrentContext == null)
			{
				CurrentContext = ctx;
				if (frame != null)
					frame.Visible = false;

			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void ExitTeamSandbox(bool andshutdown, bool andsave)
		{
			LogManager.LogInfo("SandboxService::ExitTeamSandbox has been called");
			if (frame != null && !andshutdown)
				frame.Visible = false;
			if (andsave)
				CurrentContext.Save();
			if (andshutdown)
				GameManager.Shutdown();
			else
				CurrentContext = null;
		}
		public SandboxContext[] RecollectAllContexts()
		{
			List<SandboxContext> ctxs = [];
			FileInfo[] fi = new DirectoryInfo("./sandboxes/").GetFiles();

			for (int i = 0; i < fi.Length; i++)
			{
				var file = fi[i];
				var sc = JsonSerializer.Deserialize<SandboxContext>(
					file.OpenRead().ReadToEnd(), SerializationManager.DefaultJSON);
				if (sc != null)
					ctxs.Add(sc);
			}

			return ctxs.ToArray();
		}
	}
	public class SandboxContext
	{
		public string Name;
		public string Author;
		public int Version;
		public string ContentUrl;

		public void Load(DataModel dm) => dm.Load(ContentUrl);
		public void Save(DataModel sm)
		{

		}
	}
}
