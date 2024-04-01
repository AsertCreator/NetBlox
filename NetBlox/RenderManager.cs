global using Font = Raylib_cs.Font;
using ImGuiNET;
using NetBlox.GUI;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;

namespace NetBlox
{
	public static class RenderManager
	{
		public static List<GUI.GUI> ScreenGUI = new();
		public static List<Func<int>> Coroutines = new();
		public static GUI.GUI? CurrentTeleportGUI;
		public static int PreferredFPS = 60;
		public static int ScreenSizeX = 1600;
		public static int ScreenSizeY = 900;
		public static bool DisableAllGuis = false;
		public static Thread? RenderThread;
		public static Skybox? CurrentSkybox;
		public static Camera3D MainCamera;
		public static Texture2D StudTexture;
		public static Font MainFont;
		public static long Framecount;
		public static Exception? AutomaticThrowup;

		public static void Initialize()
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);

			RenderThread = new(() =>
			{
				Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "netblox");
				Raylib.SetTargetFPS(PreferredFPS);
				Raylib.SetExitKey(KeyboardKey.Null);

				MainFont = Raylib.LoadFont(GameManager.ContentFolder + "fonts/arialbd.ttf");
				StudTexture = Raylib.LoadTexture(GameManager.ContentFolder + "textures/stud.png");
				CurrentSkybox = Skybox.LoadSkybox("bluecloud");

				rlImGui.Setup();

				while (!GameManager.ShuttingDown)
				{
					Framecount++;

					// render world
					Raylib.BeginDrawing();
					{
						Raylib.ClearBackground(new Color(102, 191, 255, 255));

						try
						{
							Raylib.BeginMode3D(MainCamera);

							int a = Raylib.GetKeyPressed();
							while (a != 0)
							{
								if (GameManager.Verbs.TryGetValue((char)a, out Action? act))
									act();
								a = Raylib.GetKeyPressed();
							}

							RenderWorld();

							if (AutomaticThrowup != null)
							{
								var t = AutomaticThrowup;
								AutomaticThrowup = null;
								throw t;
							}
						}
						catch (Exception ex)
						{
							ScreenGUI.Add(new GUI.GUI()
							{
								Elements = {
									new GUIFrame(new UDim2(0.25f, 0.175f), new UDim2(0.5f, 0.5f), Color.Red),
									new GUIText("Render error: " + ex.GetType().Name + ", " + ex.Message, new UDim2(0.5f, 0.5f))
								}
							});
						}
						finally
						{
							Raylib.EndMode3D();
						}

						// render all guis
						if (!DisableAllGuis)
						{
							if (GameManager.CurrentRoot != null)
								RenderInstanceUI(GameManager.CurrentRoot);
							RenderGUIs();

							Raylib.DrawTextEx(MainFont, $"NetBlox, version {GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}",
								new Vector2(5, 5 + 16 * 1), 16, 0, Color.White);
							Raylib.DrawTextEx(MainFont, $"Stats: instance count: {GameManager.AllInstances.Count}, fps: {Raylib.GetFPS()}",
								new Vector2(5, 5 + 16 * 2), 16, 0, Color.White);

							DebugView();
						}
					}
					Raylib.EndDrawing();

					// perform processing
					if (GameManager.CurrentRoot != null && GameManager.IsRunning)
					{
						GameManager.ProcessInstance(GameManager.CurrentRoot);
						GameManager.Schedule();
					}

					// run coroutines
					for (int i = 0; i < Coroutines.Count; i++)
					{
						Func<int> cor = Coroutines[i];
						if (cor() == -1) Coroutines.RemoveAt(i--);
					}

					// die
					if (Raylib.WindowShouldClose()) 
						GameManager.Shutdown();
				}

				Raylib.CloseWindow();
				rlImGui.Shutdown();

				CurrentSkybox.Unload();
			});

			RenderThread.Start();
		}
		public static bool ShowLua;
		public static string LECode = string.Empty;
		public static void DebugView()
		{
			rlImGui.Begin();

			ImGui.BeginMainMenuBar();
			if (ImGui.BeginMenu("NetBlox"))
			{
				if (ImGui.MenuItem(ShowLua ? "Close Lua executor" : "Open Lua executor"))
					ShowLua = !ShowLua;
				if (ImGui.MenuItem("Teleport to default place"))
					GameManager.TeleportToPlace(unchecked((ulong)-1));
				if (ImGui.MenuItem("Teleport to server")) { }
				if (ImGui.MenuItem("Exit")) 
					GameManager.Shutdown();
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("View"))
			{
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Help"))
			{
				ImGui.EndMenu();
			}
			ImGui.EndMainMenuBar();

			if (ShowLua)
			{
				ImGui.Begin("Lua executor");
				ImGui.SetWindowSize(new Vector2(400, 300));
				ImGui.InputTextMultiline("code", ref LECode, 256 * 1024, new Vector2(400 - 10, 300 - 50));
				if (ImGui.Button("Execute"))
				{
					LuaRuntime.Execute(LECode, 8, null, GameManager.CurrentRoot);
				}
				ImGui.End();
			}

			rlImGui.End();
		}
		public static void RenderInstanceUI(Instance inst)
		{
			var children = inst.GetChildren();

			inst.RenderUI();

			for (int i = 0; i < children.Length; i++)
			{
				var child = children[i];
				RenderInstanceUI(child);
			}
		}
		public static void RenderSkybox()
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
		public static void RenderWorld()
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
		public static void RenderInstance(Instance instance)
		{
			if (instance is BasePart)
				(instance as BasePart)!.Render();
			for (int i = 0; i < instance.GetChildren().Length; i++)
				RenderInstance(instance.GetChildren()[i]!);
		}
		public static void RenderGUIs()
		{
			for (int i = 0; i < ScreenGUI.Count; i++)
			{
				for (int j = 0; j < ScreenGUI[i].Elements.Count; j++)
					ScreenGUI[i].Elements[j].Render(ScreenSizeX, ScreenSizeY);
			}
		}
		public static void SetPreferredFPS(int fps)
		{
			PreferredFPS = fps;
			Raylib.SetTargetFPS(fps);
		}

		public static void ShowTeleportGui()
		{
			var guitext = new GUIText("Loading place...", new UDim2(0.5f, 0.5f))
			{
				Color = Color.White,
				FontSize = 24
			};
			var guiframe = new GUIFrame(new UDim2(1, 1), new UDim2(0.5f, 0.5f), Color.DarkBlue);

			CurrentTeleportGUI = new GUI.GUI()
			{
				Elements = new()
				{
					guiframe,
					guitext
				}
			};

			ScreenGUI.Add(CurrentTeleportGUI);
		}
		public static void HideTeleportGui()
		{
			var fc = Framecount;

			Coroutines.Add(() =>
			{
				(CurrentTeleportGUI!.Elements[0] as GUIFrame)!.Color.A -= 255 / 20; // very very very fucky hacky

				if (Framecount - fc == 20)
				{
					ScreenGUI.Remove(CurrentTeleportGUI);
					CurrentTeleportGUI = null;
					return -1;
				}

				return 0;
			});
		}
		public static void ShowKickMessage(string msg)
		{
			ScreenGUI.Add(new GUI.GUI()
			{
				Elements = {
					new GUIFrame(new UDim2(0.25f, 0.175f), new UDim2(0.5f, 0.5f), Color.Red),
					new GUIText("You've been kicked from this server: " + msg + ".\nYou may or may not been banned from this place.", new UDim2(0.5f, 0.5f))
				}
			});
		}
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
}
