global using Font = Raylib_cs.Font;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
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
		public bool DebugInformation = true;
		public bool DisableAllGuis = false;
		public bool RenderAtAll = false;
		public bool DoPostProcessing = true;
		public Skybox? CurrentSkybox;
		public Camera3D MainCamera;
		public Texture2D StudTexture;
		public Font MainFont;
		private bool SkipWindowCreation = false;
		private DataModel Root => GameManager.CurrentRoot;

		public static Queue<(string, Action<Texture2D>)> TextureLoadQueue = [];
		public static Queue<(string, Action<Shader>)> ShaderLoadQueue = [];
		public static Queue<(string, Action<Sound>)> SoundLoadQueue = [];
		public static Queue<(string, Action<Font>)> FontLoadQueue = [];

		public static Dictionary<string, Texture2D> TextureCache = [];
		public static Dictionary<string, Shader> ShaderCache = [];
		public static Dictionary<string, Sound> SoundCache = [];
		public static Dictionary<string, Font> FontCache = [];

		public int LoadBatchSize = 5;

		public unsafe RenderManager(GameManager gm, bool skiprinit, bool render, int vm)
		{
			GameManager = gm;
			VersionMargin = vm;
			GameManager.RenderManager = this;
			SkipWindowCreation = skiprinit;

			if (!skiprinit)
				Initialize(render);
			else
			{
				MainCamera = new(new Vector3(0, 15, 15), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);
				RenderAtAll = render;

				if (render)
				{
					LoadFont("rbxasset://fonts/arialbd.ttf", x => MainFont = x);
					LoadTexture("rbxasset://textures/stud.png", x => StudTexture = x);
					CurrentSkybox = Skybox.LoadSkybox(GameManager, "bluecloud");
				}
			}
		}
		public unsafe void Initialize(bool render)
		{
			MainCamera = new(new Vector3(15, 15, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 90, CameraProjection.Perspective);
			RenderAtAll = render;

			if (render)
			{
				// Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint | GameManager.CustomFlags);
				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, "NetBlox");
				Raylib.InitAudioDevice();
				Raylib.SetTargetFPS(AppManager.PreferredFPS);
				Raylib.SetExitKey(KeyboardKey.Null);
				// Raylib.SetWindowIcon(Raylib.LoadImage("./content/favicon.ico"));

				LoadFont("rbxasset://fonts/arialbd.ttf", x => MainFont = x);
				LoadTexture("rbxasset://textures/stud.png", x => StudTexture = x);
				CurrentSkybox = Skybox.LoadSkybox(GameManager, "bluecloud");
			}
		}
		public unsafe void RenderFrame()
		{
			if (RenderAtAll)
			{
				ScreenSizeX = Raylib.GetScreenWidth();
				ScreenSizeY = Raylib.GetScreenHeight();
			}
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

					PerformResourceLoading();

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

						if (DoPostProcessing) // sounds too fancy
						{
							TimeOfDay = TimeOfDay % 24;
							if (TimeOfDay != 12)
								Raylib.DrawRectangle(0, 0, ScreenSizeX, ScreenSizeY, new Color(0, 0, 0, Math.Abs(255 - (int)((TimeOfDay / 12 * 255) * 0.8 + 255 * 0.2))));
						}

						// render all guis
						if (!DisableAllGuis)
						{
							if (Root != null)
							{
								RenderInstanceUI(Root.FindFirstChild("Workspace"));
								RenderInstanceUI(Root.GetService<CoreGui>());
							}

							Raylib.DrawTextEx(MainFont, Status, new Vector2(20, 20), 16, 0, Color.White);
						}

						if (PostRender != null)
							PostRender();

						if (DebugInformation)
						{
							Raylib.DrawTextEx(MainFont, GameManager.ManagerName + ", fps: " + Raylib.GetFPS() + ", instances: " + GameManager.AllInstances.Count + 
								", task scheduler pressure: " + TaskScheduler.PressureType + " (" + TaskScheduler.JobCount + ")" + ", outgoing traffic in bytes/sec: " +
								GameManager.NetworkManager.OutgoingTraffic, 
								new(5, 5), 16, 0, Color.White);
						}

						if (!GameManager.ShuttingDown)
							Raylib.EndDrawing();
					}

					if (Raylib.WindowShouldClose() && !SkipWindowCreation)
						GameManager.Shutdown();
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
		public void PlaySound(Sound sound) => Raylib.PlaySound(sound);
		public void StopSound(Sound sound) => Raylib.StopSound(sound);
		public bool IsSoundPlaying(Sound sound) => Raylib.IsSoundPlaying(sound);
		public void Unload()
		{
			if (!SkipWindowCreation)
				Raylib.CloseWindow();
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

			var pos = MainCamera.Position;
			var ss = CurrentSkybox.SkyboxSize;
			var ass = CurrentSkybox.SkyboxSize * 0.9965f; // hehe

			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ass, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Left);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ass, 0, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Right);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ass, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Bottom);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ass, 0) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Top);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ass) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Front);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ass) + pos, Vector3.Zero, ss, ss, ss, Color.White, Faces.Back);
		}
		public void RenderWorld()
		{
			// i should probably avoid using ifs in these moments, but who cares if its like 5 nanoseconds?
			if (Root == null) return;

			var skypos = MainCamera.Position;
			var works = Root.FindFirstChild("Workspace");

			RenderSkybox();

			if (CurrentSkybox != null && CurrentSkybox.SkyboxWires)
				Raylib.DrawCubeWires(skypos, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, Color.Blue);

			if (works != null)
				RenderInstance(works);
		}
		public void RenderInstance(Instance instance)
		{
			if (instance is I3DRenderable)
				(instance as I3DRenderable)!.Render();
			for (int i = 0; i < instance.GetChildren().Length; i++)
				RenderInstance(instance.GetChildren()[i]!);
		}
		public void ShowKickMessage(string msg)
		{
			Status = "You've been kicked from this server: " + msg + ".\nYou may or may not been banned from this place.";
		}
		public void PerformResourceLoading() // e F f I c I e N t  resource loader
		{
			for (int i = 0; i < LoadBatchSize; i++)
			{
				if (TextureLoadQueue.Count > 0)
				{
					var el = TextureLoadQueue.Dequeue();
					var x = AppManager.ResolveUrlAsync(el.Item1, true);
					x.Wait();
					{
						try
						{
							var tex = Raylib.LoadTexture(x.Result);
							TextureCache[el.Item1] = tex;
							el.Item2(tex);
						}
						catch
						{
							LogManager.LogWarn("Could not load texture from " + el.Item1);
							return;
						}
					};
				}
				if (FontLoadQueue.Count > 0)
				{
					var el = FontLoadQueue.Dequeue();
					var x = AppManager.ResolveUrlAsync(el.Item1, true);
					x.Wait();
					{
						try
						{
							var font = Raylib.LoadFont(x.Result);
							FontCache[el.Item1] = font;
							el.Item2(font);
						}
						catch
						{
							LogManager.LogWarn("Could not load font from " + el.Item1);
							return;
						}
					};
				}
				if (ShaderLoadQueue.Count > 0)
				{
					var el = ShaderLoadQueue.Dequeue();
					var x = AppManager.ResolveUrlAsync(el.Item1, true);
					x.Wait();
					{
						try
						{
							var shader = Raylib.LoadShader(x.Result + ".vs", x.Result + ".fs");
							ShaderCache[el.Item1] = shader;
							el.Item2(shader);
						}
						catch
						{
							LogManager.LogWarn("Could not load shader from " + el.Item1);
							return;
						}
					};
				}
				if (SoundLoadQueue.Count > 0)
				{
					var el = SoundLoadQueue.Dequeue();
					var x = AppManager.ResolveUrlAsync(el.Item1, true);
					x.Wait();
					{
						try
						{
							var snd = Raylib.LoadSound(x.Result);
							SoundCache[el.Item1] = snd;
							el.Item2(snd);
						}
						catch
						{
							LogManager.LogWarn("Could not load sound from " + el.Item1);
							return;
						}
					};
				}
			}
		}
		public static void LoadTexture(string path, Action<Texture2D> callback)
		{
			if (TextureCache.TryGetValue(path, out var tex))
				callback(tex);
			else
				TextureLoadQueue.Enqueue((path, callback));
		}
		public static void LoadFont(string path, Action<Font> callback)
		{
			if (FontCache.TryGetValue(path, out var tex))
				callback(tex);
			else
				FontLoadQueue.Enqueue((path, callback));
		}
		public static void LoadShader(string path, Action<Shader> callback)
		{
			if (ShaderCache.TryGetValue(path, out var tex))
				callback(tex);
			else
				ShaderLoadQueue.Enqueue((path, callback));
		}
		public static void LoadSound(string path, Action<Sound> callback)
		{
			if (SoundCache.TryGetValue(path, out var tex))
				callback(tex);
			else
				SoundLoadQueue.Enqueue((path, callback));
		}
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
}
