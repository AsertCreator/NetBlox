global using Font = Raylib_cs.Font;
using ImGuiNET;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using rlImGui_cs;
using System.Net;
using System.Numerics;

namespace NetBlox
{
	public static class RenderManager
	{
		public static List<Func<int>> Coroutines = new();
		public static int PreferredFPS = 60;
		public static int ScreenSizeX = 1600;
		public static int ScreenSizeY = 900;
		public static string Status = string.Empty;
		public static bool DisableAllGuis = false;
		public static Thread? RenderThread;
		public static Skybox? CurrentSkybox;
		public static Camera3D MainCamera;
		public static Texture2D StudTexture;
		public static Font MainFont;
		public static long Framecount;

		public static void Initialize(bool render)
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);

			RenderThread = new(() =>
			{
				if (render)
				{
					Raylib.SetTraceLogLevel(TraceLogLevel.None);
					Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);

					Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "netblox");
					Raylib.SetTargetFPS(PreferredFPS);
					Raylib.SetExitKey(KeyboardKey.Null);

					MainFont = Raylib.LoadFont(GameManager.ContentFolder + "fonts/arialbd.ttf");
					StudTexture = Raylib.LoadTexture(GameManager.ContentFolder + "textures/stud.png");
					CurrentSkybox = Skybox.LoadSkybox("bluecloud");

					rlImGui.Setup(true, true);
				}

				while (!GameManager.ShuttingDown)
				{
					if (render)
					{
						ScreenSizeX = Raylib.GetScreenWidth();
						ScreenSizeY = Raylib.GetScreenHeight();
					}

					Framecount++;
					try
					{
						if (render)
						{
							if (NetworkManager.IsServer && Raylib.IsMouseButtonDown(MouseButton.Right))
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
								Raylib.ClearBackground(new Color(102, 191, 255, 255));

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
										RenderInstanceUI(GameManager.CurrentRoot.FindFirstChild("Workspace"));

									Raylib.DrawTextEx(MainFont, $"NetBlox {(NetworkManager.IsServer ? "Server" : "Client")}, version {GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}",
										new Vector2(5, 5 + 16 * 1), 16, 0, Color.White);
									Raylib.DrawTextEx(MainFont, $"Stats: instance count: {GameManager.AllInstances.Count}, fps: {Raylib.GetFPS()}",
										new Vector2(5, 5 + 16 * 2), 16, 0, Color.White);
									Raylib.DrawTextEx(MainFont, Status,
										new Vector2(5, 5 + 16 * 3), 16, 0, Color.White);

									DebugView();
								}

								Raylib.EndDrawing();
                            }

                            if (Raylib.WindowShouldClose())
                                GameManager.Shutdown();
                        }

						// perform processing
						if (GameManager.CurrentRoot != null && GameManager.IsRunning)
							GameManager.ProcessInstance(GameManager.CurrentRoot);

                        if (GameManager.SpecialRoot != null && GameManager.IsRunning)
                            GameManager.ProcessInstance(GameManager.SpecialRoot);

						if ((GameManager.SpecialRoot != null) ||
							(GameManager.CurrentRoot != null && GameManager.IsRunning))
                            GameManager.Schedule();

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

				if (render)
				{
					Raylib.CloseWindow();
					rlImGui.Shutdown();

					CurrentSkybox.Unload();
				}
			});

			RenderThread.Start();
		}
		public static class DebugViewInfo
		{
			public static bool ShowLua = true;
			public static bool ShowSC = true;
			public static bool ShowOutput = true;
			public static string LECode = string.Empty;
			public static string SCAddress = "127.0.0.1";
		}
		public static void DebugView()
		{
			rlImGui.Begin();

			var v = ImGui.GetMainViewport();
			var flags = ImGuiWindowFlags.NoBringToFrontOnFocus |
				ImGuiWindowFlags.NoNavFocus |
				ImGuiWindowFlags.NoDocking |
				ImGuiWindowFlags.NoTitleBar |
				ImGuiWindowFlags.NoResize |
				ImGuiWindowFlags.NoMove |
				ImGuiWindowFlags.NoCollapse |
				ImGuiWindowFlags.MenuBar |
				ImGuiWindowFlags.NoBackground;
			var pv = ImGui.GetStyle().WindowPadding;

			ImGui.Begin("#root", flags);
			ImGui.SetWindowPos(-pv);
			ImGui.SetWindowSize(new Vector2(ScreenSizeX, ScreenSizeY) + pv * 2);

			ImGui.DockSpace(v.ID, new(0.0f, 0.0f), ImGuiDockNodeFlags.PassthruCentralNode);
			ImGui.BeginMainMenuBar();
			if (ImGui.BeginMenu("NetBlox"))
			{
				if (ImGui.MenuItem(DebugViewInfo.ShowLua ? "Close Lua executor" : "Open Lua executor"))
					DebugViewInfo.ShowLua = !DebugViewInfo.ShowLua;
				if (ImGui.MenuItem("Teleport to default place"))
					GameManager.TeleportToPlace(unchecked((ulong)-1));
				if (ImGui.MenuItem("Teleport to server"))
					DebugViewInfo.ShowSC = !DebugViewInfo.ShowSC;
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

			ImGui.End();
			{
				ImGui.Begin("Instance tree viewer");
				void Node(Instance ins)
				{
					string msg = ins.Name + " - " + ins.ClassName;
					if (ImGui.TreeNode(msg))
					{
						for (int i = 0; i < ins.Children.Count; i++)
						{
							Node(ins.Children[i]);
						}
						ImGui.TreePop();
                    }
                }
				if (GameManager.CurrentRoot != null)
					Node(GameManager.CurrentRoot);
                if (GameManager.SpecialRoot != null)
                    Node(GameManager.SpecialRoot);

                ImGui.End();
			}
			if (DebugViewInfo.ShowLua)
			{
				ImGui.Begin("Lua executor");
				ImGui.SetWindowSize(new Vector2(400, 300));
				ImGui.InputTextMultiline("", ref DebugViewInfo.LECode, 256 * 1024, new Vector2(400 - 12, 300 - 55));
				if (ImGui.Button("Execute"))
				{
					LuaRuntime.Execute(DebugViewInfo.LECode, 8, null, GameManager.CurrentRoot);
				}
				ImGui.End();
			}
			if (DebugViewInfo.ShowSC)
			{
				ImGui.Begin("Teleport to server");
				ImGui.InputText("Address", ref DebugViewInfo.SCAddress, 256);
				ImGui.InputText("Username", ref GameManager.Username, 256);
				if (ImGui.Button("Connect"))
				{
					NetworkManager.ConnectToServer(IPAddress.Parse(DebugViewInfo.SCAddress));
				}
				ImGui.End();
			}
			if (DebugViewInfo.ShowOutput)
			{
				string log = LogManager.Log.ToString();
				ImGui.Begin("Output");
				ImGui.InputTextMultiline("", ref log, (uint)(log.Length + 50), ImGui.GetWindowSize(), ImGuiInputTextFlags.ReadOnly);
				ImGui.End();
			}

			rlImGui.End();
		}
		public static void RenderInstanceUI(Instance? inst)
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
		public static void SetPreferredFPS(int fps)
		{
			PreferredFPS = fps;
			Raylib.SetTargetFPS(fps);
		}
		public static void ShowKickMessage(string msg)
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
