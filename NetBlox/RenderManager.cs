global using Font = Raylib_cs.Font;
using ImGuiNET;
using Raylib_cs;
using NetBlox.Instances;
using NetBlox.Runtime;
using rlImGui_cs;
using System.Numerics;

namespace NetBlox
{
	public static class RenderManager
	{
		public static List<GUI.GUI> ScreenGUI = new();
		public static List<Func<int>> Coroutines = new();
		public static int ScreenSizeX = 1600;
		public static int ScreenSizeY = 900;
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
				Raylib.SetTargetFPS(GameManager.PreferredFPS);
				Raylib.SetExitKey(KeyboardKey.Null);

				MainFont = Raylib.LoadFont(PlayManager.ContentFolder + "fonts/arialbd.ttf");
				StudTexture = Raylib.LoadTexture(PlayManager.ContentFolder + "textures/stud.png");
				CurrentSkybox = Skybox.LoadSkybox("bluecloud");

				rlImGui.Setup();

				while (!GameManager.ShuttingDown)
				{
					Framecount++;

					Raylib.BeginDrawing();
					Raylib.ClearBackground(new Color(102, 191, 255, 255));

					Raylib.BeginMode3D(MainCamera);

					int a = Raylib.GetKeyPressed();
					while (a != 0)
					{
						if (PlayManager.Verbs.TryGetValue((char)a, out Action? act)) 
							act();
						a = Raylib.GetKeyPressed();
					}

					try
					{
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
						ScreenGUI.Add(new GUI.GUI() {
							CorrespondingPhase = GameManager.CurrentGameplayPhase,
							Elements = {
								new GUI.GUIFrame(new Structs.UDim2(0.25f, 0.175f), new Structs.UDim2(0.5f, 0.5f), Color.Red),
								new GUI.GUIText("Render error: " + ex.GetType().Name + ", " + ex.Message, new Structs.UDim2(0.5f, 0.5f))
							}
						});
					}
					finally
					{
						Raylib.EndMode3D();
					}

					if (GameManager.CurrentRoot != null)
						RenderInstanceUI(GameManager.CurrentRoot);
					RenderGUIs();

					Raylib.DrawTextEx(MainFont, $"NetBlox, version {AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}", 
						new Vector2(5, 5 + 16 * 1), 16, 0, Color.White);
					Raylib.DrawTextEx(MainFont, $"Stats: instance count: {GameManager.AllInstances.Count}, fps: {Raylib.GetFPS()}", 
						new Vector2(5, 5 + 16 * 2), 16, 0, Color.White);

					DebugView();

					Raylib.EndDrawing();

					GameManager.MessageQueue.Enqueue(new Message()
					{
						Type = MessageType.Timer,
						Number = Framecount
					});

					for (int i = 0; i < Coroutines.Count; i++)
					{
						Func<int> cor = Coroutines[i];
						if (cor() == -1) Coroutines.RemoveAt(i--);
					}

					if (Raylib.WindowShouldClose())
						GameManager.MessageQueue.Enqueue(new Message()
						{
							Type = MessageType.Shutdown
						});
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
				if (ImGui.MenuItem("Exit"))
					GameManager.MessageQueue.Enqueue(new Message()
					{
						Type = MessageType.Shutdown
					});
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

			DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ss, 0, 0) + pos, ss, ss, ss, Color.White, Faces.Left);
			DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ss, 0, 0) + pos, ss, ss, ss, Color.White, Faces.Right);
			DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ss, 0) + pos, ss, ss, ss, Color.White, Faces.Bottom);
			DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ss, 0) + pos, ss, ss, ss, Color.White, Faces.Top);
			DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ss) + pos, ss, ss, ss, Color.White, Faces.Front);
			DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ss) + pos, ss, ss, ss, Color.White, Faces.Back);
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
			if (instance.IsA(nameof(BasePart))) 
				((BasePart)instance).Render();
			for (int i = 0; i < instance.GetChildren().Length; i++)
				RenderInstance(instance.GetChildren()[i]!);
		}
		public static void RenderGUIs()
		{
			if (GameManager.CurrentGameplayPhase != GameplayPhase.Black)
			{
				for (int i = 0; i < ScreenGUI.Count; i++)
				{
					if (ScreenGUI[i].CorrespondingPhase != GameManager.CurrentGameplayPhase) continue;
					for (int j = 0; j < ScreenGUI[i].Elements.Count; j++)
						ScreenGUI[i].Elements[j].Render(ScreenSizeX, ScreenSizeY);
				}
			}
		}
		public static void DrawCubeTextureRec(Texture2D texture, Vector3 position, float width, float height, float length, Color color, Faces f, bool tile = false)
		{
			if (f != 0)
			{
				float x = position.X;
				float y = position.Y;
				float z = position.Z;

				Rlgl.SetTexture(texture.Id);

				Rlgl.Begin(7);
				Rlgl.Color4ub(color.R, color.G, color.B, color.A);

				Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_REPEAT);
				Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_REPEAT);

				// NOTE: Enable texture 1 for Front, Back
				Rlgl.EnableTexture(texture.Id);

				if (f.HasFlag(Faces.Front))
				{
					// Front Face
					// Normal Pointing Towards Viewer
					Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Back))
				{
					// Back Face
					// Normal Pointing Away From Viewer
					Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Top))
				{
					// Top Face
					// Normal Pointing Up
					Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Bottom))
				{
					// Bottom Face
					// Normal Pointing Down
					Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Right))
				{
					// Right face
					// Normal Pointing Right
					Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Left))
				{
					// Left Face
					// Normal Pointing Left
					Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

					// Bottom Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Bottom Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

					// Top Left Of The Texture and Quad
					Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
				}

				Rlgl.End();

				Rlgl.DisableTexture();
			}
		}
		public static void DrawCubeFaced(Vector3 position, float width, float height, float length, Color color, Faces f, bool tile = false)
		{
			if (f != 0)
			{
				float x = position.X;
				float y = position.Y;
				float z = position.Z;

				Rlgl.Begin(7);
				Rlgl.Color4ub(color.R, color.G, color.B, color.A);

				if (f.HasFlag(Faces.Front))
				{
					Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Back))
				{
					Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Top))
				{
					Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Bottom))
				{
					Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Right))
				{
					Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Left))
				{
					Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
				}

				Rlgl.End();
			}
		}
		public static float DistanceFrom(Vector3 vect, Vector3 vect2)
		{
			return MathF.Sqrt((vect.X - vect2.X) * (vect.X - vect2.X) +
					(vect.Y - vect2.Y) * (vect.Y - vect2.Y) +
					(vect.Z - vect2.Z) * (vect.Z - vect2.Z));
		}
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
	public class Skybox
	{
		public bool SkyboxWires = false;
		public bool SkyboxMoves = true;
		public int SkyboxSize = 999;
		public Texture2D Top;
		public Texture2D Bottom;
		public Texture2D Left;
		public Texture2D Right;
		public Texture2D Front;
		public Texture2D Back;

		private Skybox() { }

		public static Skybox LoadSkybox(string fp)
		{
			Skybox sb = new();

			sb.Back = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_bk.png");
			sb.Bottom = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_dn.png");
			sb.Front = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_ft.png");
			sb.Left = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_lf.png");
			sb.Right = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_rt.png");
			sb.Top = Raylib.LoadTexture(PlayManager.ContentFolder + $"skybox/{fp}_up.png");

			return sb;
		}
		public void Unload()
		{
			Raylib.UnloadTexture(Front);
			Raylib.UnloadTexture(Top);
			Raylib.UnloadTexture(Left);
			Raylib.UnloadTexture(Right);
			Raylib.UnloadTexture(Bottom);
			Raylib.UnloadTexture(Back);
		}
	}
}
