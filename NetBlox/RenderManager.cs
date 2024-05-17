global using Font = Raylib_cs.Font;
using ImGuiNET;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using rlImGui_cs;
using System.Net;
using System.Numerics;
using System.Reflection;

namespace NetBlox
{
	public class RenderManager
	{
		public GameManager GameManager;
		public Action? PostRender;
		public List<Func<int>> Coroutines = new();
		public List<Shader> Shaders = new();
		public int ScreenSizeX = 1600;
		public int ScreenSizeY = 900;
		public int VersionMargin = 0;
		public string Status = string.Empty;
		public bool DisableAllGuis = false;
		public bool RenderAtAll = false;
		public Skybox? CurrentSkybox;
		public Camera3D MainCamera;
		public Texture2D StudTexture;
		public Shader LightingShader;
		public Font MainFont;
		public long Framecount;

		public unsafe RenderManager(GameManager gm, bool skiprinit, bool render, int vm)
		{
			GameManager = gm;
			VersionMargin = vm;

			if (!skiprinit)
				Initialize(render);
			else
			{
				MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);
				RenderAtAll = render;

				if (render)
				{
					MainFont = ResourceManager.GetFont(AppManager.ContentFolder + "fonts/arialbd.ttf");
					StudTexture = ResourceManager.GetTexture(AppManager.ContentFolder + "textures/stud.png");
					CurrentSkybox = Skybox.LoadSkybox("bluecloud");
					LightingShader = LoadShader(AppManager.ResolveUrl("rbxasset://shaders/lighting"));

					int ambientLoc = Raylib.GetShaderLocation(LightingShader, "ambient");
					LightingShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(LightingShader, "viewPos");
					Raylib.SetShaderValue(LightingShader, ambientLoc, new float[] { 0.1f, 0.1f, 0.1f, 1.0f }, ShaderUniformDataType.Vec4);
				}
			}
		}
		public unsafe void Initialize(bool render)
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);
			RenderAtAll = render;

			if (render)
			{
				Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);

				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "netblox");
				Raylib.SetTargetFPS(AppManager.PreferredFPS);
				Raylib.SetExitKey(KeyboardKey.Null);

				MainFont = ResourceManager.GetFont(AppManager.ContentFolder + "fonts/arialbd.ttf");
				StudTexture = ResourceManager.GetTexture(AppManager.ContentFolder + "textures/stud.png");
				CurrentSkybox = Skybox.LoadSkybox("bluecloud");
				LightingShader = LoadShader(AppManager.ResolveUrl("rbxasset://shaders/lighting"));

				int ambientLoc = Raylib.GetShaderLocation(LightingShader, "ambient");
				LightingShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(LightingShader, "viewPos");
				Raylib.SetShaderValue(LightingShader, ambientLoc, new float[] { 0.1f, 0.1f, 0.1f, 1.0f }, ShaderUniformDataType.Vec4);

				rlImGui.Setup(true, true);
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
					if (GameManager.NetworkManager.IsServer && Raylib.IsMouseButtonDown(MouseButton.Right))
					{
						Raylib.UpdateCamera(ref MainCamera, CameraMode.FirstPerson);
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
					}

					// render world
					Raylib.BeginDrawing();
					{
						Raylib.ClearBackground(Color.SkyBlue);

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

						// render all guis
						if (!DisableAllGuis)
						{
							if (GameManager.CurrentRoot != null)
							{
								RenderInstanceUI(GameManager.CurrentRoot.FindFirstChild("Workspace"));
								RenderUI(GameManager.CurrentRoot.GetService<CoreGui>());
							}

							Raylib.DrawTextEx(MainFont, $"NetBlox {(GameManager.IsStudio ? "StudioManager" : (GameManager.NetworkManager.IsServer ? "Server" : "Client"))}, version {AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}",
								new Vector2(5, 5 + 16 * (0 + VersionMargin)), 16, 0, Color.White);
							Raylib.DrawTextEx(MainFont, $"Stats: instance count: {GameManager.AllInstances.Count}, fps: {Raylib.GetFPS()}, manager name: {GameManager.ManagerName}",
								new Vector2(5, 5 + 16 * (1 + VersionMargin)), 16, 0, Color.White);
							Raylib.DrawTextEx(MainFont, Status,
								new Vector2(5, 5 + 16 * (2 + VersionMargin)), 16, 0, Color.White);
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

				GameManager.ProcessInstance(GameManager.CurrentRoot);
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
				rlImGui.Shutdown();

				CurrentSkybox.Unload();
			}

			foreach (var shader in Shaders)
				Raylib.UnloadShader(shader);
		}
		private struct Light
		{
			public int type;
			public bool enabled;
			public Vector3 position;
			public Vector3 target;
			public Color color;
			public float attenuation;

			// Shader locations
			public int enabledLoc;
			public int typeLoc;
			public int positionLoc;
			public int targetLoc;
			public int colorLoc;
			public int attenuationLoc;
		}
		public unsafe void SetLight(int i, int type, Vector3 position, Vector3 target, Color color)
		{
			Light light = new();

			light.enabled = true;
			light.type = type;
			light.position = position;
			light.target = target;
			light.color = color;
			light.enabledLoc = Raylib.GetShaderLocation(LightingShader, "lights[" + i + "].enabled");
			light.typeLoc = Raylib.GetShaderLocation(LightingShader, "lights[" + i + "].type");
			light.positionLoc = Raylib.GetShaderLocation(LightingShader, "lights[" + i + "].position");
			light.targetLoc = Raylib.GetShaderLocation(LightingShader, "lights[" + i + "].target");
			light.colorLoc = Raylib.GetShaderLocation(LightingShader, "lights[" + i + " ].color");

			// Send to shader light enabled state and type
			Raylib.SetShaderValue(LightingShader, light.enabledLoc, &light.enabled, ShaderUniformDataType.Int);
			Raylib.SetShaderValue(LightingShader, light.typeLoc, &light.type, ShaderUniformDataType.Int);

			// Send to shader light position values
			float[] pf = { light.position.X, light.position.Y, light.position.Z };
			Raylib.SetShaderValue(LightingShader, light.positionLoc, pf, ShaderUniformDataType.Vec3);

			// Send to shader light target position values
			float[] tf = { light.position.X, light.position.Y, light.position.Z };
			Raylib.SetShaderValue(LightingShader, light.targetLoc, tf, ShaderUniformDataType.Vec3);

			// Send to shader light color values
			float[] cf = { light.color.R/255f, light.color.G/255f, light.color.B/255f, light.color.A/255f };
			Raylib.SetShaderValue(LightingShader, light.colorLoc, cf, ShaderUniformDataType.Vec4);
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
		public void RenderUI(Instance? inst)
		{
			if (inst == null) return;
			var children = inst.GetChildren();

			for (int i = 0; i < children.Length; i++)
			{
				var child = children[i];
				child.RenderUI();
			}
		}
		public void RenderSkybox()
		{
			if (CurrentSkybox == null) return;

			var pos = MainCamera.Position;
			var ss = CurrentSkybox.SkyboxSize;

			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ss, 0, 0) + pos, ss, ss, ss, Color.White, Faces.Left);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ss, 0, 0) + pos, ss, ss, ss, Color.White, Faces.Right);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ss, 0) + pos, ss, ss, ss, Color.White, Faces.Bottom);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ss, 0) + pos, ss, ss, ss, Color.White, Faces.Top);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ss) + pos, ss, ss, ss, Color.White, Faces.Front);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ss) + pos, ss, ss, ss, Color.White, Faces.Back);
		}
		public void RenderWorld()
		{
			// i should probably avoid using ifs in these moments, but who cares if its like 5 nanoseconds?
			if (GameManager.CurrentRoot == null) return;

			var skypos = MainCamera.Position;
			var works = GameManager.CurrentRoot.FindFirstChild("Workspace");

			RenderSkybox();

			if (CurrentSkybox != null && CurrentSkybox.SkyboxWires)
				Raylib.DrawCubeWires(skypos, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, Color.Blue);

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
		public Shader LoadShader(string f)
		{
			var s = Raylib.LoadShader(f + ".vs", f + ".fss");
			Shaders.Add(s);
			return s;
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
