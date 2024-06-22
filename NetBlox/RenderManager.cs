global using Font = Raylib_CsLo.Font;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_CsLo;
using System.Numerics;

namespace NetBlox
{
	public sealed class RenderManager
	{
		public GameManager GameManager;
		public Action? PostRender;
		public List<Func<int>> Coroutines = new();
		public List<Shader> Shaders = new();
		public int ScreenSizeX = 1600;
		public int ScreenSizeY = 900;
		public int VersionMargin = 0;
		public double TimeOfDay = 12;
		public string Status = string.Empty;
		public bool DebugInformation = false;
		public bool DisableAllGuis = false;
		public bool RenderAtAll = false;
		public bool DoPostProcessing = true;
		public Skybox? CurrentSkybox;
		public Camera3D MainCamera;
		public Texture StudTexture;
		public Font MainFont;
		public long Framecount;
		private DataModel Root => GameManager.CurrentRoot;

		public unsafe RenderManager(GameManager gm, bool skiprinit, bool render, int vm)
		{
			GameManager = gm;
			VersionMargin = vm;
			GameManager.RenderManager = this;

			if (!skiprinit)
				Initialize(render);
			else
			{
				MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.CAMERA_PERSPECTIVE);
				RenderAtAll = render;

				if (render)
				{
					MainFont = ResourceManager.GetFont(AppManager.ContentFolder + "fonts/arialbd.ttf");
					StudTexture = ResourceManager.GetTexture(AppManager.ContentFolder + "textures/stud.png");
					CurrentSkybox = Skybox.LoadSkybox("bluecloud");
				}
			}
		}
		public unsafe void Initialize(bool render)
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.CAMERA_PERSPECTIVE);
			RenderAtAll = render;

			if (render)
			{
				// Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_MSAA_4X_HINT | GameManager.CustomFlags);
				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "netblox");
				Raylib.SetTargetFPS(AppManager.PreferredFPS);
				Raylib.SetExitKey(KeyboardKey.KEY_NULL);
				// Raylib.SetWindowIcon(Raylib.LoadImage("./content/favicon.ico"));

				MainFont = ResourceManager.GetFont(AppManager.ContentFolder + "fonts/arialbd.ttf");
				StudTexture = ResourceManager.GetTexture(AppManager.ContentFolder + "textures/stud.png");
				CurrentSkybox = Skybox.LoadSkybox("bluecloud");
			}
		}
		public unsafe void RenderFrame()
		{
			if (RenderAtAll)
			{
				ScreenSizeX = Raylib.GetScreenWidth();
				ScreenSizeY = Raylib.GetScreenHeight();
			}

			Framecount++;
			try
			{
				if (RenderAtAll)
				{
					if (GameManager.NetworkManager.IsServer && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
					{
						Raylib.UpdateCamera(ref MainCamera);
						if (Raylib.IsKeyDown(KeyboardKey.KEY_SPACE))
						{
							MainCamera.target.Y += 0.1f;
							MainCamera.position.Y += 0.1f;
						}
						if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
						{
							MainCamera.target.Y -= 0.1f;
							MainCamera.position.Y -= 0.1f;
						}
					}

					// render world
					Raylib.BeginDrawing();
					{
						Raylib.ClearBackground(Raylib.SKYBLUE);
						Raylib.BeginMode3D(MainCamera);

						int a = Raylib.GetKeyPressed();
						while (a != 0)
						{
							if (GameManager.Verbs.TryGetValue((char)a, out Action? act))
								act();
							a = Raylib.GetKeyPressed();
						}

						RenderWorld();

						Raylib.EndMode3D();

						if (DoPostProcessing) // sounds too fancy
						{
							TimeOfDay = TimeOfDay % 24;
							if (TimeOfDay != 12)
								Raylib.DrawRectangle(0, 0, ScreenSizeX, ScreenSizeY, new Color(0, 0, 0, Math.Abs(255 - (int)((TimeOfDay / 12 * 255) * 0.8 + 255 * 0.2))));
						}

						if (DebugInformation)
						{
							Raylib.DrawTextEx(MainFont, GameManager.ManagerName + ", fps: " + Raylib.GetFPS() + ", instances: " + GameManager.AllInstances.Count, new(5, 5), 16, 0, Raylib.WHITE);
						}

						// render all guis
						if (!DisableAllGuis)
						{
							if (Root != null)
							{
								RenderInstanceUI(Root.FindFirstChild("Workspace"));
								RenderInstanceUI(Root.GetService<CoreGui>());
							}

							Raylib.DrawTextEx(MainFont, Status, new Vector2(20, 20), 16, 0, Raylib.WHITE);
						}

						if (PostRender != null)
							PostRender();

						if (!GameManager.ShuttingDown)
							Raylib.EndDrawing();
					}

					if (Raylib.WindowShouldClose())
						GameManager.Shutdown();
				}

				// run coroutines
				for (int i = 0; i < Coroutines.Count; i++)
				{
					Func<int> cor = Coroutines[i];
					if (cor() == -1) Coroutines.RemoveAt(i--);
				}

				GameManager.ProcessInstance(Root);
			}
			catch (Exception ex)
			{
				Status = "Render error: " + ex.GetType().Name + ", " + ex.Message;
			}
		}
		public void Unload()
		{
			if (RenderAtAll)
			{
				Raylib.CloseWindow();
				CurrentSkybox.Unload();
			}

			foreach (var shader in Shaders)
				Raylib.UnloadShader(shader);
		}
		public void RenderInstanceUI(Instance? inst)
		{
			if (inst == null) return;
			var children = inst.GetChildren();

			inst.RenderUI();

			for (int i = 0; i < children.Length; i++)
			{
				var child = children[i];
				RenderInstanceUI(child);
			}
		}
		public void RenderSkybox()
		{
			if (CurrentSkybox == null) return;

			var pos = MainCamera.position;
			var ss = CurrentSkybox.SkyboxSize;
			var ass = CurrentSkybox.SkyboxSize * 0.9965f; // hehe

			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ass, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Left);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ass, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Right);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ass, 0) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Bottom);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ass, 0) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Top);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ass) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Front);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ass) + pos, Vector3.Zero, ss, ss, ss, Raylib.WHITE, Faces.Back);
		}
		public void RenderWorld()
		{
			// i should probably avoid using ifs in these moments, but who cares if its like 5 nanoseconds?
			if (Root == null) return;

			var skypos = MainCamera.position;
			var works = Root.FindFirstChild("Workspace");

			RenderSkybox();

			if (CurrentSkybox != null && CurrentSkybox.SkyboxWires)
				Raylib.DrawCubeWires(skypos, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, Raylib.BLUE);

			if (works != null)
				RenderInstance(works);
		}
		public void RenderInstance(Instance instance)
		{
			if (instance is BasePart)
				(instance as BasePart)!.Render();
			for (int i = 0; i < instance.GetChildren().Length; i++)
				RenderInstance(instance.GetChildren()[i]!);
		}
		public void ShowKickMessage(string msg)
		{
			Status = "You've been kicked from this server: " + msg + ".\nYou may or may not been banned from this place.";
		}
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
}
