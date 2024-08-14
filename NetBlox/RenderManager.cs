global using Font = Raylib_cs.Font;
using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Services;
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
		public string? CurrentMessage = string.Empty;
		public string? CurrentHint = string.Empty;
		public bool DebugInformation = true;
		public bool DisableAllGuis = false;
		public bool RenderAtAll = false;
		public bool DoPostProcessing = true;
		public Skybox? CurrentSkybox;
		public Camera3D MainCamera;
		public Texture2D StudTexture;
		public Font MainFont;
		public BasePart? MoverPart;
		public bool FirstFrame = true;
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
					for (int i = 0; i < GameManager.Verbs.Count; i++)
					{
						var verb = GameManager.Verbs.ElementAt(i);
						if (Raylib.IsKeyPressed(verb.Key))
							verb.Value();
					}

					for (int i = 0; i < 2; i++) // s p e e d
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
							if (Raylib.IsKeyDown(KeyboardKey.G))
							{
								Part part = new(GameManager);
								part.Name = "Trash";
								part.Parent = Root.GetService<Workspace>();
								part.Position = MainCamera.Position;
								part.Size = new(1, 1, 1);
								part.Color3 = Color.DarkPurple;
							}
						}
					}
					
					PerformResourceLoading();

					// render world
					Raylib.BeginDrawing();
					{
						if (FirstFrame)
							GameManager.PhysicsManager.Begin();

						FirstFrame = false;

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

						// render all guis
						if (!DisableAllGuis)
						{
							if (Root != null)
							{
								RenderInstanceUI(Root.GetService<Workspace>(true));

								if (CurrentHint != null)
								{
									Raylib.DrawRectangle(0, ScreenSizeY - 26, ScreenSizeX, 26, Color.Black); // quite bold of me to assume that top 30 pixels are used.
									var v = Raylib.MeasureTextEx(MainFont, CurrentHint, MainFont.BaseSize / 1.5f, 0);
									Raylib.DrawTextEx(MainFont, CurrentHint, new(ScreenSizeX / 2 - v.X / 2, ScreenSizeY - 26 + 15 + 9 - v.Y), MainFont.BaseSize / 1.5f, 0, Color.White);
								}

								RenderPlayerGui();
								RenderInstanceUI(Root.GetService<CoreGui>());
								RenderInstanceUI(Root.GetService<SandboxService>());
							}

							Raylib.DrawTextEx(MainFont, Status, new Vector2(20, 20), 16, 0, Color.White);
						}

						if (PostRender != null)
							PostRender();

						if (DebugInformation)
						{
							Raylib.DrawTextEx(MainFont, GameManager.ManagerName + ", fps: " + Raylib.GetFPS() + ", instances: " + GameManager.AllInstances.Count + 
								", task scheduler pressure: " + TaskScheduler.PressureType + " (" + TaskScheduler.JobCount + ")" + ", outgoing traffic: " +
								MathE.FormatSize(GameManager.NetworkManager.OutgoingTraffic) + (GameManager.PhysicsManager.DisablePhysics ? "" : ", physics enabled"), 
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
		public void RenderPlayerGui()
		{
			if (GameManager.NetworkManager.IsClient)
			{
				var plrs = Root.GetService<Players>(true);
				if (plrs == null) return;
				var lp = plrs.LocalPlayer;
				if (lp == null) return;
				var ch = ((Player)lp).FindFirstChild("PlayerGui");
				if (ch == null) return;
				RenderInstanceUI(ch);
			}
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
			var works = Root.GetService<Workspace>(true);
			var sand = Root.GetService<SandboxService>();

			RenderSkybox();

			if (CurrentSkybox != null && CurrentSkybox.SkyboxWires)
				Raylib.DrawCubeWires(skypos, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, Color.Blue);

			if (works != null)
				RenderInstance(works);
			if (sand != null)
				RenderInstance(sand);
		}
		public void RenderInstance(Instance instance)
		{
			var c = instance.GetChildren();
			(instance as I3DRenderable)?.Render();
			for (int i = 0; i < c.Length; i++)
				RenderInstance(c[i]!);
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
					try
					{
						var x = AppManager.ResolveUrlAsync(el.Item1, true);
						x.Wait();
						{
							var tex = Raylib.LoadTexture(x.Result);
							TextureCache[el.Item1] = tex;
							el.Item2(tex);
						};
					}
					catch
					{
						LogManager.LogWarn("Could not load texture from " + el.Item1);
						return;
					}
				}
				if (FontLoadQueue.Count > 0)
				{
					var el = FontLoadQueue.Dequeue();
					try
					{
						var x = AppManager.ResolveUrlAsync(el.Item1, true);
						x.Wait();
						{
							var font = Raylib.LoadFont(x.Result);
							FontCache[el.Item1] = font;
							el.Item2(font);
						};
					}
					catch
					{
						LogManager.LogWarn("Could not load font from " + el.Item1);
						return;
					}
				}
				if (ShaderLoadQueue.Count > 0)
				{
					var el = ShaderLoadQueue.Dequeue();
					try
					{
						var x = AppManager.ResolveUrlAsync(el.Item1, true);
						x.Wait();
						{
							var shader = Raylib.LoadShader(x.Result + ".vs", x.Result + ".fs");
							ShaderCache[el.Item1] = shader;
							el.Item2(shader);
						};
					}
					catch
					{
						LogManager.LogWarn("Could not load shader from " + el.Item1);
						return;
					}
				}
				if (SoundLoadQueue.Count > 0)
				{
					var el = SoundLoadQueue.Dequeue();
					var x = AppManager.ResolveUrlAsync(el.Item1, true);
					try
					{
						x.Wait();
						{
							var snd = Raylib.LoadSound(x.Result);
							SoundCache[el.Item1] = snd;
							el.Item2(snd);
						};
					}
					catch
					{
						LogManager.LogWarn("Could not load sound from " + el.Item1);
						return;
					}
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
