global using Font = Raylib_cs.Font;
using Raylib_cs;
using System.Numerics;

namespace PhysicsTest
{
	public static class RenderManager
	{
		public static Action? PostRender;
		public static List<Func<int>> Coroutines = new();
		public static List<Shader> Shaders = new();
		public static int ScreenSizeX = 1600;
		public static int ScreenSizeY = 900;
		public static int VersionMargin = 0;
		public static double TimeOfDay = 12;
		public static string Status = string.Empty;
		public static bool DisableAllGuis = false;
		public static bool RenderAtAll = false;
		public static bool DoPostProcessing = true;
		public static Skybox? CurrentSkybox;
		public static Camera3D MainCamera;
		public static Texture2D StudTexture;
		public static Font MainFont;
		public static long Framecount;

		public static void Initialize(bool render)
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);
			RenderAtAll = render;

			if (render)
			{
				// Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "netblox physics test");
				Raylib.SetTargetFPS(60);
				Raylib.SetExitKey(KeyboardKey.Null);
				// Raylib.SetWindowIcon(Raylib.LoadImage("./content/favicon.ico"));

				MainFont = ResourceManager.GetFont("./content/fonts/arialbd.ttf");
				StudTexture = ResourceManager.GetTexture("./content/textures/stud.png");
				CurrentSkybox = Skybox.LoadSkybox("bluecloud");
			}
		}
		public static void RenderFrame()
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
					if (Raylib.IsMouseButtonDown(MouseButton.Right))
					{
						Raylib.UpdateCamera(ref MainCamera, CameraMode.FirstPerson);
					}
					if (Raylib.IsKeyDown(KeyboardKey.Space))
					{
						MainCamera.Target.Y += 0.1f;
						MainCamera.Position.Y += 0.1f;
					}
					if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
					{
						MainCamera.Target.Y -= 0.1f;
						MainCamera.Position.Y -= 0.1f;
					}

					// render world
					Raylib.BeginDrawing();
					{
						Raylib.ClearBackground(Color.SkyBlue);
						Raylib.BeginMode3D(MainCamera);

						RenderWorld();

						Raylib.EndMode3D();

						if (DoPostProcessing) // sounds too fancy
						{
							TimeOfDay = TimeOfDay % 24;
							if (TimeOfDay != 12)
								Raylib.DrawRectangle(0, 0, ScreenSizeX, ScreenSizeY, new Color(0, 0, 0, Math.Abs(255 - (int)((TimeOfDay / 12 * 255) * 0.8 + 255 * 0.2))));
						}

						if (PostRender != null)
							PostRender();

						Raylib.EndDrawing();
					}

					if (Raylib.WindowShouldClose())
					{
						return;
					}
				}

				// run coroutines
				for (int i = 0; i < Coroutines.Count; i++)
				{
					Func<int> cor = Coroutines[i];
					if (cor() == -1) Coroutines.RemoveAt(i--);
				}
			}
			catch (Exception ex)
			{
				Status = "Render error: " + ex.GetType().Name + ", " + ex.Message;
			}
		}
		public static void RenderSkybox()
		{
			if (CurrentSkybox == null) return;

			var pos = MainCamera.Position;
			var ss = CurrentSkybox.SkyboxSize;

			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ss, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Left);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ss, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Right);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ss, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Bottom);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ss, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Top);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ss) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Front);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ss) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Back);
		}
		public static void RenderWorld()
		{
			RenderSkybox();
			for (int i = 0; i < Program.AllActors.Count; i++)
			{
				var actor = Program.AllActors[i];
				RenderUtils.DrawCubeTextureRec(StudTexture, actor.Position, actor.Rotation, actor.Size.X, actor.Size.Y, actor.Size.Z, actor.Color, Faces.All, true);
			}
		}
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
}
