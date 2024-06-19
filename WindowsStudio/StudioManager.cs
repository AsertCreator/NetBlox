using ImGuiNET;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_CsLo;
using rlImGui_cs;
using System.Net;
using System.Numerics;

namespace NetBlox.Studio
{
	public static class StudioManager
	{
		public class DebugViewInfo
		{
			public static bool EnableDebugView = true;
			public static bool ShowLua = false;
			public static bool ShowITS = false;
			public static bool ShowSC = false;
			public static bool ShowOutput = false;
			public static Dictionary<BaseScript, bool> ShowScriptSource = new();
			public static string LECode = string.Empty;
			public static string SCAddress = "127.0.0.1";
		}
		public static GameManager? TitleGame;
		public static GameManager? EditorGame;
		public static int Main(string[] args)
		{
			Raylib.SetTraceLogLevel(TraceLogLevel.None);
			AppManager.LoadFastFlags(args);

			var v = RlGl.GetVersion();

			if (v == GlVersion.OpenGl11 || v == GlVersion.OpenGl21)
			{
				Console.WriteLine("NetBlox cannot run on your device, because the OpenGL 3.3 isn't supported. Consider re-checking your system settings.");
				return 1;
			}

			LogManager.LogInfo($"NetBlox Studio ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			CreateTitleGame();
			AppManager.SetRenderTarget(TitleGame);
			AppManager.Start();

			return 0;
		}
		public static GameManager CreateEditorGame(string? path)
		{
			EditorGame = AppManager.CreateGame(new()
			{
				AsStudio = true,
				AsServer = true,
				ProhibitProcessing = true,
				SkipWindowCreation = true,
				GameName = "EditorManager",
				VersionMargin = 1
			}, 
			[], (x, y) =>
			{
				if (path == null)
				{
					var works = y.CurrentRoot.GetService<Workspace>();
					var part = new Part(y);

					part.Size = new(60, 5, 60);
					part.Position = Vector3.Zero;
					part.Color = Color.DarkGreen;
					part.Parent = works;

					works.ZoomToExtents();
				}
				else
					new Project(y.CurrentRoot, true, path);

				var em = new EditorManager(y.RenderManager);
			});
			EditorGame.ProhibitScripts = true;
			return EditorGame;
		}
		public static GameManager CreateTitleGame()
		{
			TitleGame = AppManager.CreateGame(new()
			{
				AsStudio = true,
				ProhibitScripts = true,
				GameName = "Title"
			}, 
			[], (x, y) =>
			{
				var works = y.CurrentRoot.GetService<Workspace>();
				var part = new Part(y);

				part.Size = new(60, 5, 60);
				part.Position = Vector3.Zero;
				part.Color = Color.DarkGreen;
				part.Parent = works;

				works.ZoomToExtents();

				y.RenderManager.PostRender = () =>
				{
					rlImGui.Begin();
					ImGui.Begin("NetBlox Studio");
					ImGui.SetWindowSize(new Vector2(400, 200));
					ImGui.SetWindowPos(new Vector2(Raylib.GetScreenWidth() / 2 - 200, Raylib.GetScreenHeight() / 2 - 100));
					// i hate you, ctrl+r
					ImGui.Text($"Welcome to NetBlox Studio! ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch})");
					ImGui.Dummy(new Vector2(0, 7));
					if (ImGui.Button("Create Baseplate project")) OpenProject(null);
					if (ImGui.Button("Create Empty project")) OpenProject(null);
					if (ImGui.Button("Open existing project")) OpenExisting();
					if (ImGui.Button("Exit")) { AppManager.Shutdown(); return; }

					ImGui.End();
					rlImGui.End();

					y.RenderManager.MainCamera.Target = part.Position;
					Raylib.CameraMoveRight(ref y.RenderManager.MainCamera, 0.1f, true);
				};
			});
			TitleGame.ProhibitScripts = true;
			return TitleGame;
		}
		public unsafe static void OpenExisting()
		{
			Thread th = new(() => // because fucking com exists
			{
				var nw = new NativeWindow();
				using var ofd = new OpenFileDialog();

				nw.AssignHandle(new nint(Raylib.GetWindowHandle()));
				ofd.Title = "Select RBXL file";
				ofd.Filter = "Project files (.rbxl)|*.rbxl";

				if (ofd.ShowDialog(nw) == DialogResult.OK)
					OpenProject(ofd.FileName);
			});
			th.SetApartmentState(ApartmentState.STA);
			th.Start();
		}
		public static void OpenProject(string? path)
		{
			LogManager.LogInfo("Loading project " + path ?? "<null>");
			CreateEditorGame(path);
			AppManager.SetRenderTarget(EditorGame);
		}
	}
}
