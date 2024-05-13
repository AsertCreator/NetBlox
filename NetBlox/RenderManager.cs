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

namespace NetBlox
{
	public static class RenderManager
	{
		public static List<Func<int>> Coroutines = new();
		public static List<Shader> Shaders = new();
		public static int PreferredFPS = 60;
		public static int ScreenSizeX = 1600;
		public static int ScreenSizeY = 900;
		public static string Status = string.Empty;
		public static bool DisableAllGuis = false;
		public static Thread? RenderThread;
		public static Skybox? CurrentSkybox;
		public static Camera3D MainCamera;
		public static Texture2D StudTexture;
		public static Shader LightingShader;
		public static Font MainFont;
		public static long Framecount;

		public unsafe static void Initialize(bool render)
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
					LightingShader = LoadShader(GameManager.ResolveUrl("rbxasset://shaders/lighting"));

					int ambientLoc = Raylib.GetShaderLocation(LightingShader, "ambient");
					LightingShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(LightingShader, "viewPos");
					Raylib.SetShaderValue(LightingShader, ambientLoc, new float[] { 0.1f, 0.1f, 0.1f, 1.0f }, ShaderUniformDataType.Vec4);

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

							float[] cameraPos = [MainCamera.Position.X, MainCamera.Position.Y, MainCamera.Position.Z];
							Raylib.SetShaderValue(LightingShader, LightingShader.Locs[(int)ShaderLocationIndex.VectorView], cameraPos, ShaderUniformDataType.Vec3);

							// render world
							Raylib.BeginDrawing();
							{
								Raylib.ClearBackground(Color.SkyBlue);

								Raylib.BeginMode3D(MainCamera);
								Raylib.BeginShaderMode(LightingShader);

								int a = Raylib.GetKeyPressed();
								while (a != 0)
								{
									if (GameManager.Verbs.TryGetValue((char)a, out Action? act))
										act();
									a = Raylib.GetKeyPressed();
								}

								RenderWorld();

								Raylib.EndShaderMode();
								Raylib.EndMode3D();

								// render all guis
								if (!DisableAllGuis)
								{
									if (GameManager.CurrentRoot != null)
									{
										RenderInstanceUI(GameManager.CurrentRoot.FindFirstChild("Workspace"));
										RenderUI(GameManager.CurrentRoot.GetService<CoreGui>());
									}

									Raylib.DrawTextEx(MainFont, $"NetBlox {(NetworkManager.IsServer ? "Server" : "Client")}, version {GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}",
										new Vector2(5, 5 + 16 * 0), 16, 0, Color.White);
									Raylib.DrawTextEx(MainFont, $"Stats: instance count: {GameManager.AllInstances.Count}, fps: {Raylib.GetFPS()}",
										new Vector2(5, 5 + 16 * 1), 16, 0, Color.White);
									Raylib.DrawTextEx(MainFont, Status,
										new Vector2(5, 5 + 16 * 2), 16, 0, Color.White);

									if (DebugViewInfo.EnableDebugView)
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

						if (GameManager.CurrentRoot != null && GameManager.IsRunning)
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

					foreach (var shader in Shaders)
						Raylib.UnloadShader(shader);
				}
			});

			RenderThread.Start();
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
		public static unsafe void SetLight(int i, int type, Vector3 position, Vector3 target, Color color)
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
		public static class DebugViewInfo
		{
			public static bool EnableDebugView = false;
			public static bool ShowLua = false;
			public static bool ShowITS = false;
			public static bool ShowSC = false;
			public static bool ShowOutput = false;
			public static Dictionary<BaseScript, bool> ShowScriptSource = new();
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
				if (ImGui.MenuItem("Show output"))
					DebugViewInfo.ShowOutput = !DebugViewInfo.ShowOutput;
				if (ImGui.MenuItem("Show instance tree viewer"))
					DebugViewInfo.ShowITS = !DebugViewInfo.ShowITS;
				if (ImGui.MenuItem("Give yourself build tools"))
				{
					for (int i = 0; i < 5; i++)
					{
						HopperBin hb = new();
						hb.BinType = i;
						hb.Parent = GameManager.CurrentRoot.GetService<Players>().LocalPlayer.FindFirstChild("Backpack");
					}
				}
				if (ImGui.MenuItem("Kill the entire thing"))
				{
					Environment.FailFast("NetBlox had died, lol");
				}
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
			if (DebugViewInfo.ShowITS)
			{
				ImGui.Begin("Instance tree viewer");
				void Node(Instance ins)
				{
					string msg = ins.Name + " - " + ins.ClassName;
					bool open = ImGui.TreeNode(msg);
					if (ImGui.BeginPopupContextItem())
					{
						if (ImGui.MenuItem("Destroy"))
						{
							ins.Destroy();
						}
						if (ins is BaseScript && ImGui.MenuItem("Show script's source code"))
						{
							var bs = ins as BaseScript;
							if (DebugViewInfo.ShowScriptSource.ContainsKey(bs!))
								DebugViewInfo.ShowScriptSource[bs!] = !DebugViewInfo.ShowScriptSource[bs!];
							else
								DebugViewInfo.ShowScriptSource[bs!] = true;
						}
						if (ins is BaseScript && ImGui.MenuItem("Re-execute"))
						{
							var bs = ins as BaseScript;
							bs.HadExecuted = false;
						}
						ImGui.EndPopup();
					}
					if (open)
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

				ImGui.End();
			}
			foreach (var kvp in DebugViewInfo.ShowScriptSource)
			{
				if (kvp.Value)
				{
					var sou = kvp.Key.Source;
					ImGui.SetWindowSize(new(400, 500));
					ImGui.Begin("Script Viewer - " + kvp.Key.GetFullName());
					ImGui.InputTextMultiline("", ref sou, (uint)kvp.Key.Source.Length, ImGui.GetWindowSize(), ImGuiInputTextFlags.ReadOnly);
					ImGui.End();
				}
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
		public static void RenderUI(Instance? inst)
		{
			if (inst == null) return;
			var children = inst.GetChildren();

			for (int i = 0; i < children.Length; i++)
			{
				var child = children[i];
				child.RenderUI();
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

			// if (works != null)
			// 	RenderSkybox();

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
		public static Shader LoadShader(string f)
		{
			var s = Raylib.LoadShader(f + ".vs", f + ".fss");
			Shaders.Add(s);
			return s;
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
