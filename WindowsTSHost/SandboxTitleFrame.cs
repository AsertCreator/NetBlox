using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances.GUIs
{
	public class SandboxTitleFrame : GuiObject
	{
		private bool init = false;
		private SandboxContext[] ctxs;
		private FileSystemWatcher watcher;

		public SandboxTitleFrame(GameManager ins) : base(ins)
		{
			watcher = new("sandboxes");

			void UpdateContexts(object sender, EventArgs e)
			{
				var ss = Root.GetService<SandboxService>(true);
				if (ss == null)
					return;
				ctxs = ss.RecollectAllContexts();
			}

			watcher.Created += UpdateContexts;
			watcher.Deleted += UpdateContexts;
			watcher.Changed += UpdateContexts;
			watcher.EnableRaisingEvents = true;
		}

		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			base.RenderGUI(cp, cs);
			var p = Position.Calculate(cp, cs);
			var s = Size.Calculate(cp, cs);
			var ss = Root.GetService<SandboxService>(true);

			if (ss == null)
				return;

			void DrawString(string txt, int x, int y) => 
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, txt, new Vector2(x, y), 16, 16 / 25, Color.White);
			bool DrawHyperlink(string txt, int x, int y)
			{
				var sz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, txt, 16, 16 / 25);
				var pos = new Vector2(x, y);
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, txt, pos, 16, 16 / 25, Color.White);
				return RenderUtils.MouseCollides(pos, sz) && Raylib.IsMouseButtonPressed(MouseButton.Left);
			}
			void DrawStringSized(string txt, int x, int y, float size) => 
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, txt, new Vector2(x, y), size, size / 25, Color.White);

			Raylib.DrawRectangle((int)p.X, (int)p.Y, (int)s.X, (int)s.Y, new Raylib_cs.Color(0, 0, 0, 127));
			DrawStringSized("Welcome to NetBlox Team Sandbox", 25, 25, 24);

			if (!init)
			{
				init = true;
				ctxs = ss.RecollectAllContexts();
			}

			if (DrawHyperlink("+ Create new sandbox", 25, 75))
			{
				LogManager.LogWarn("User initited a process of creating a new sandbox");
			}

			for (int i = 0; i < ctxs.Length; i++)
			{
				var sc = ctxs[i];
				if (DrawHyperlink(sc.Name, 25, (i + 1) * GameManager.RenderManager.MainFont.BaseSize * 2 + 75))
				{
					LogManager.LogWarn("hey");
				}
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(SandboxTitleFrame) == classname) return true;
			return base.IsA(classname);
		}
	}
}
