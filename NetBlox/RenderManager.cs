global using Font = Raylib_cs.Font;
using MoonSharp.Interpreter;
using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Instances.Parts;
using NetBlox.Network;
using NetBlox.Structs;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rectangle = Raylib_cs.Rectangle;
using NetBlox.Rendering;

namespace NetBlox
{
	public class CrispFont
	{
		public Font SpriteFont;
		public int FontSize;
		public string FontFamily;

		public CrispFont(int fontsize, string fontfamily)
		{
			var font = AppManager.ResolveUrlAsync("rbxasset://fonts/" + fontfamily, false).WaitAndGetResult();
			SpriteFont = Raylib.LoadFontEx(font, fontsize, null, 95);
			FontFamily = fontfamily;
			FontSize = fontsize;
			Raylib.SetTextureFilter(SpriteFont.Texture, TextureFilter.Point);
		}
		~CrispFont()
		{
			Raylib.UnloadFont(SpriteFont);
		}
	}
	public sealed class RenderManager
	{
		public GameManager GameManager;
		public Action? PostRender;
		public List<Func<int>> Coroutines = [];
		public List<Shader> Shaders = [];
		public static List<CrispFont> CrispFonts = [];
		public int ScreenSizeX = 1600;
		public int ScreenSizeY = 900;
		public double TimeOfDay = 12;
		public string Status = string.Empty;
		public string? CurrentMessage = null;
		public string? CurrentHint = null;
		public bool DebugInformation = true;
		public bool DisableAllGuis = false;
		public bool DisableParticles = false;
		public bool RenderAtAll = false;
		public bool DoPostProcessing = true;
		public bool WhiteOut = true;
		public bool DoRenderDebugCharts = false;
		public Skybox? CurrentSkybox;
		public Camera3D MainCamera;
		public Texture2D StudTexture;
		public Texture2D BlankTexture;
		public CrispFont MainFont;
		public CrispFont MainFont14;
		public Camera CurrentCamera;
		public Instances.GUIs.TextBox? FocusedBox;
		public bool FirstFrame = true;
		public Thread? FrustumCullingThread;
		public bool FrustumCullingPaused = true;
		public bool UnlimitFramerate = false;
		public bool LogFrameRendering = false;
		public Texture2D? Cursor;
		public RenderShadingManager RenderShadingManager;

		public HashSet<I3DRenderable> Visibles3DGrade0 = new();
		public HashSet<I3DRenderable> Visibles3DGrade1 = new();
		public HashSet<Instance> Visibles2D = new();

		private readonly bool SkipWindowCreation = false;
		private Stopwatch renderStopwatch = new();
		private DataModel Root => GameManager.CurrentRoot;

		public static Queue<(string, Action<Texture2D>)> TextureLoadQueue = [];
		public static Queue<(string, Action<Mesh>)> MeshLoadQueue = [];
		public static Queue<(string, Action<Shader>)> ShaderLoadQueue = [];
		public static Queue<(string, Action<Sound>)> SoundLoadQueue = [];

		public static Dictionary<string, Texture2D> TextureCache = [];
		public static Dictionary<string, Mesh> MeshCache = [];
		public static Dictionary<string, Shader> ShaderCache = [];
		public static Dictionary<string, Sound> SoundCache = [];

		public static Job? ResourceLoadingTask;

		public Shader? ActiveShader;

		public int LoadBatchSize = 5;

		public List<Particle> AllParticles = [];

		public unsafe RenderManager(GameManager gm, bool skipWindowCreation, bool renderAtAll)
		{
			GameManager = gm;
			GameManager.RenderManager = this;
			SkipWindowCreation = skipWindowCreation;
			RenderAtAll = renderAtAll;

			LogFrameRendering = AppManager.GetFastFlag("FFlagLogFrameRendering", false);
			MainCamera = new(new Vector3(5, 6, 0), Vector3.Zero, Vector3.UnitY, 90, CameraProjection.Perspective);

			CurrentCamera = new Camera(gm);

			if (!skipWindowCreation)
			{
				if (!Raylib.IsWindowReady())
					CreateWindow();

				if (renderAtAll)
				{
					RenderShadingManager = new(this);
					InitializeResources();

					if (ResourceLoadingTask == null)
					{
						ResourceLoadingTask = TaskScheduler.ScheduleNamedJob("ResourceLoadingTask", JobType.Renderer, _ =>
						{
							if (GameManager.ShuttingDown)
								return JobResult.CompletedSuccess;

							PerformResourceLoading();

							return JobResult.NotCompleted;
						});
						ResourceLoadingTask.JobTimingContext.Priority = 3;
					}
				}
			}
		}
		public void CreateWindow()
		{
			try
			{
				// Raylib.SetTraceLogLevel(TraceLogLevel.None);
				Raylib.SetTargetFPS(10000);
				Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint | GameManager.CustomFlags);
				Raylib.InitWindow(ScreenSizeX, ScreenSizeY, GameManager.ClientStartupInfo == null ? "NetBlox" : GameManager.ClientStartupInfo.WindowName);
				Raylib.InitAudioDevice();
				Raylib.SetExitKey(KeyboardKey.Null);
				Raylib.SetWindowIcon(Raylib.LoadImage(AppManager.ResolveUrlAsync("rbxasset://textures/menu.png", false).WaitAndGetResult()));
			}
			catch (Exception ex)
			{
				LogManager.LogError("Failed to initialize the window: " + ex.GetType() + ", msg: " + ex.Message);
				throw;
			}
		}
		public void InitializeResources()
		{
			try
			{
				LogManager.LogInfo("Initializing graphical resources...");

				MainFont = GetCrispFont(16, "arialbd.ttf");
				MainFont14 = GetCrispFont(14, "arialbd.ttf");

				CurrentSkybox = Skybox.LoadSkybox(GameManager, "bluecloud");

				LoadTexture("rbxasset://textures/blank.png", x => BlankTexture = x);
				LoadTexture("rbxasset://textures/stud.png", x => StudTexture = x);

				if (GameManager.ServerStartupInfo == null) // maybe nm is not initialized by now (im lazy to check)
					RenderShadingManager.SwitchToMode(RenderShadingMode.SmoothShading);
			}
			catch (Exception ex)
			{
				LogManager.LogError("Failed to initialize the resources: " + ex.GetType() + ", msg: " + ex.Message);
				throw;
			}
		}
		public void PollForVerbsAndExecute()
		{
			for (int i = 0; i < GameManager.Verbs.Count; i++)
			{
				var verb = GameManager.Verbs.ElementAt(i);

				if (Raylib.IsKeyPressed(verb.Key))
				{
					TaskScheduler.ScheduleNamedJob("VerbJob", JobType.Miscellaneous, _ =>
					{
						verb.Value();
						return JobResult.CompletedSuccess;
					});
				}
			}
		}
		public void DoServerCameraControl()
		{
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
						Part part = new(GameManager)
						{
							Name = "Trash",
							Parent = Root.GetService<Workspace>(true),
							Position = MainCamera.Position,
							Size = new(1, 1, 1),
							Color3 = Color.DarkPurple
						};
						GameManager.NetworkManager.AddReplication(part, Replication.REPM_TOALL, Replication.REPW_NEWINST);
					}
				}
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
					PollForVerbsAndExecute();
					DoServerCameraControl();

					GameManager.CurrentRunService.PreRender.Fire(DynValue.NewNumber(renderStopwatch.Elapsed.TotalSeconds));
					renderStopwatch.Reset();
					renderStopwatch.Start();

					// renderAtAll world if it exists
					if (Root != null)
					{
						if (LogFrameRendering)
							LogManager.LogInfo("Beginning renderAtAll...");

						Raylib.BeginDrawing();
						{
							FirstFrame = false;

							Raylib.ClearBackground(Color.SkyBlue);
							Raylib.BeginMode3D(MainCamera);

							RenderWorld();

							Raylib.EndMode3D();

							if (DoPostProcessing) // sounds too fancy
							{
								TimeOfDay %= 24;
								if (TimeOfDay != 12)
									Raylib.DrawRectangle(0, 0, ScreenSizeX, ScreenSizeY, new Color(0, 0, 0, Math.Abs(255 - (int)((TimeOfDay / 12 * 255 * 0.8) + (255 * 0.2)))));
							}

							// renderAtAll all guis
							if (!DisableAllGuis)
							{
								if (Root != null)
								{
									RenderInstanceUI(Root.GetService<Workspace>(true));

									if (CurrentHint != null)
									{
										Raylib.DrawRectangle(0, ScreenSizeY - 26, ScreenSizeX, 26, Color.Black);
										var v = Raylib.MeasureTextEx(MainFont.SpriteFont, CurrentHint, MainFont.SpriteFont.BaseSize, 0);
										Raylib.DrawTextEx(MainFont.SpriteFont, CurrentHint, new((ScreenSizeX / 2) - (v.X / 2), ScreenSizeY - 26 + 15 + 9 - v.Y), MainFont.SpriteFont.BaseSize, 0, Color.White);
									}

									if (GameManager.NetworkManager.IsClient)
									{
										RenderPlayerGui();
										RenderInstanceUI(Root.GetService<CoreGui>());
										RenderInstanceUI(Root.GetService<SandboxService>());
									}
								}

								if (DoRenderDebugCharts)
									RenderDebugCharts();

								Raylib.DrawTextEx(MainFont.SpriteFont, Status, new Vector2(20, 20), 16, 0, Color.White);
							}

							if (Cursor.HasValue)
							{
								Raylib.DrawTexturePro(Cursor.Value, new Rectangle(0, 0, Cursor.Value.Width, Cursor.Value.Height), 
									new Rectangle(Raylib.GetMousePosition(), new Vector2(24, 24)), new Vector2(), 0, Color.White);
							}

							PostRender?.Invoke();

							if (DebugInformation)
							{
								var debugstring = GameManager.GameName +
									", fps: " + Raylib.GetFPS() +
									", instances: " + GameManager.AllInstances.Count +
									", task scheduler pressure: " + TaskScheduler.JobCount +
									", outgoing traffic: " + MathE.FormatSize(GameManager.NetworkManager.OutgoingTraffic) +
									(GameManager.PhysicsManager.DisablePhysics ? "" : ", physics enabled") +
									", actors count: " + GameManager.PhysicsManager.Actors.Count;

								if (GameManager.NetworkManager.IsClient)
								{
									debugstring +=
										", CSSending count: " + GameManager.NetworkManager.ServerboundPendingSendPackets.Count +
										", CSProcess count: " + GameManager.NetworkManager.ClientboundPendingProcessPackets.Count;
								}

								Raylib.DrawTextEx(MainFont.SpriteFont, debugstring, new(5, ScreenSizeY - 16 - 5), 16, 0, Color.White);
							}

							if (WhiteOut)
								Raylib.ClearBackground(Color.White);

							if (!GameManager.ShuttingDown)
								Raylib.EndDrawing();
						}

						if (LogFrameRendering)
							LogManager.LogInfo("Ending renderAtAll...");
					}

					renderStopwatch.Stop();

					if (!UnlimitFramerate)
					{
						var leftRenderTime = 1000 / AppManager.PreferredFPS - renderStopwatch.Elapsed.TotalMilliseconds;
						if (leftRenderTime > 0)
							TaskScheduler.CurrentJob.JobTimingContext.JoinedUntil = DateTime.UtcNow.AddMilliseconds(leftRenderTime);
					}

					GameManager.CurrentRunService.RenderStepped.Fire(DynValue.NewNumber(renderStopwatch.Elapsed.TotalSeconds));

					if (Raylib.WindowShouldClose() && !SkipWindowCreation)
					{
						TaskScheduler.ScheduleNamedJob("UserClosedWindowJob", JobType.Miscellaneous, _ =>
						{
							GameManager.Shutdown();
							return JobResult.CompletedSuccess;
						});
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
				var status = "Render error: " + ex.GetType().Name + ", " + ex.Message;
				LogManager.LogError(status);
				Status = status;
			}
		}
		public CrispFont GetCrispFont(int fontsize, string fontfamily)
		{
			var font = CrispFonts.Find(x => x.FontSize == fontsize && x.FontFamily == fontfamily);
			if (font != null)
				return font;
			font = new CrispFont(fontsize, fontfamily);
			CrispFonts.Add(font);
			return font;
		}
		public void PlaySound(Sound sound) => Raylib.PlaySound(sound);
		public void StopSound(Sound sound) => Raylib.StopSound(sound);
		public bool IsSoundPlaying(Sound sound) => Raylib.IsSoundPlaying(sound);
		public void Unload()
		{
			if (!SkipWindowCreation)
				Raylib.CloseWindow();
		}
		private void RenderPlayerGui()
		{
			if (GameManager.NetworkManager.IsClient)
			{
				var plrs = Root.GetService<Players>(true);
				if (plrs == null) return;

				var lp = plrs.LocalPlayer;
				if (lp == null) return;

				var ba = ((Player)lp).FindFirstChild("Backpack");
				if (ba != null)
					RenderInstanceUI(ba);

				var ch = ((Player)lp).FindFirstChild("PlayerGui");
				if (ch != null)
					RenderInstanceUI(ch);
			}
		}
		private void RenderInstanceUI(Instance? inst)
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
		private void RenderSkybox()
		{
			if (CurrentSkybox == null) return;

			var pos = MainCamera.Position;
			var ss = CurrentSkybox.SkyboxSize;
			var ass = CurrentSkybox.SkyboxSize * 0.9965f; // hehe

			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Back, new Vector3(ass, 0, 0) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Left);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Front, new Vector3(-ass, 0, 0) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Right);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Top, new Vector3(0, ass, 0) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Bottom);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Bottom, new Vector3(0, -ass, 0) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Top);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Left, new Vector3(0, 0, -ass) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Front);
			RenderUtils.DrawCubeTextureRec(CurrentSkybox.Right, new Vector3(0, 0, ass) + pos, Quaternion.Identity, ss, ss, ss, Color.White, Faces.Back);
		}
		private void RenderDebugCharts()
		{
			double overallsum = 0;
			Dictionary<Job, double> percentages = [];

			for (int i = 0; i < TaskScheduler.RunningJobs.Count; i++)
			{
				var job = TaskScheduler.RunningJobs[i];
				overallsum += job.JobTimingContext.LastCycleTime;
			}
			for (int i = 0; i < TaskScheduler.RunningJobs.Count; i++)
			{
				var job = TaskScheduler.RunningJobs[i];
				percentages[job] = job.JobTimingContext.LastCycleTime / overallsum;
			}

			var center = new Vector2(Raylib.GetScreenWidth() - 200, 200);
			float percentagePassed = 0;

			for (int i = 0; i < percentages.Count; i++)
			{
				var kvp = percentages.ElementAt(i);
				var color = BrickColor.Registry[kvp.Key.GetHashCode() % BrickColor.Registry.Length];
				var segments = (int)Math.Ceiling(360 * kvp.Value);

				Raylib.DrawCircleSector(center, 150, 360 * percentagePassed, 360 * (percentagePassed + (float)kvp.Value), 
					segments, color.Color);

				percentagePassed += (float)kvp.Value;

				Raylib.DrawTextEx(MainFont14.SpriteFont, kvp.Key.Name + " - " + kvp.Key.Type.ToString() + " - " + 
					(kvp.Value * 100).ToString("F"),
					new Vector2(Raylib.GetScreenWidth() - 350, 400 + 16 * i), 14, 1.4f, color.Color);
			}
		}
		private void RenderWorld()
		{
			if (Root == null) return;

			var skypos = MainCamera.Position;
			var works = Root.GetService<Workspace>(true);
			var sand = Root.GetService<SandboxService>();

			Shader? shader = RenderShadingManager.SupplyShader();

			RenderShadingManager.SetCameraLookAt(MainCamera.Target - MainCamera.Position);

			// this is a five step process now.

			//
			// 1) renderAtAll skybox
			//
			{
				RenderSkybox();

				if (CurrentSkybox != null && CurrentSkybox.SkyboxWires)
					Raylib.DrawCubeWires(skypos, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, CurrentSkybox.SkyboxSize, Color.Blue);
			}

			if (shader.HasValue)
				Raylib.BeginShaderMode(shader.Value);

			//
			// 2) renderAtAll 3D grade #0 objects - opaque 3d objects
			//
			{
				IEnumerator<I3DRenderable> enumerator = Visibles3DGrade0.GetEnumerator();
				while (enumerator.MoveNext())
					enumerator.Current.Render();
			}

			//
			// 3) renderAtAll particles
			//
			if (!DisableParticles)
				RenderParticles(AppManager.GetRendererDeltaTime());

			//
			// 4) renderAtAll 3D grade #1 objects - translucent 3d objects
			//
			{
				Raylib.BeginBlendMode(BlendMode.Alpha);

				IEnumerator<I3DRenderable> enumerator = Visibles3DGrade1.GetEnumerator();
				while (enumerator.MoveNext())
					enumerator.Current.Render();

				Raylib.EndBlendMode();
			}

			//
			// 5) renderAtAll 2D objects - UI objects mostly
			//
			if (!DisableAllGuis)
			{
				IEnumerator<I3DRenderable> enumerator = Visibles3DGrade0.GetEnumerator();
				while (enumerator.MoveNext())
					enumerator.Current.Render();
			}

			if (shader.HasValue)
				Raylib.EndShaderMode();
		}
		private void RenderInstance(Instance instance)
		{
			var c = instance.GetChildren();
			(instance as I3DRenderable)?.Render();

			for (int i = 0; i < c.Length; i++)
				RenderInstance(c[i]!);
		}
		public void ShowKickMessage(string msg, bool isSystemMessage = false)
		{
			if (!isSystemMessage)
				Status = "You've been kicked from this server: " + msg + ".\nYou may or may not been banned from this place.";
			else
				Status = msg;
		}
		public unsafe void PerformResourceLoading() // e F f I c I e N t  resource loader
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
							Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
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
				if (MeshLoadQueue.Count > 0)
				{
					var el = MeshLoadQueue.Dequeue();
					try
					{
						var x = AppManager.ResolveUrlAsync(el.Item1, true);
						x.Wait();
						{
							var mesh = Raylib.LoadModel(x.Result);
							if (mesh.MeshCount <= 0)
							{
								LogManager.LogWarn("Could not load mesh from " + el.Item1 + "; no meshes found at that path");
								return;
							}
							MeshCache[el.Item1] = mesh.Meshes[0];
							el.Item2(mesh.Meshes[0]);
						};
					}
					catch
					{
						LogManager.LogWarn("Could not load mesh from " + el.Item1);
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
							string? vertexShader = null;
							string? fragmentShader = null;

							if (File.Exists(x.Result.Replace(".shad", "") + ".vshad"))
								vertexShader = x.Result.Replace(".shad", "") + ".vshad";
							if (File.Exists(x.Result))
								fragmentShader = x.Result;

							if (vertexShader == null && fragmentShader == null)
							{
								LogManager.LogWarn("The shader (" + el.Item1 + ") doesn't have both vertex and fragment components, not loading!");
							}
							else
							{
								var shader = Raylib.LoadShader(vertexShader, fragmentShader);
								ShaderCache[el.Item1] = shader;
								el.Item2(shader);
							}
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
		public static void LoadMesh(string path, Action<Mesh> callback)
		{
			if (MeshCache.TryGetValue(path, out var tex))
				callback(tex);
			else
				MeshLoadQueue.Enqueue((path, callback));
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
		public void BeginFustumCullingThread()
		{
			if (FrustumCullingThread == null) 
			{
				FrustumCullingThread = new Thread(() =>
				{
					var frustum = CaptureFrustum(Raylib.GetCameraMatrix(MainCamera));

					while (!GameManager.ShuttingDown)
					{
						while (FrustumCullingPaused) Thread.Yield();
						for (int i = 0; i < GameManager.PhysicsManager.Actors.Count; i++)
						{
							var actor = GameManager.PhysicsManager.Actors[i];
							if (actor == null)
								continue;
							actor.IsCulled = IsToBeCulled(frustum, actor);
						}
					}
				});
				FrustumCullingThread.Name = "uh";
				FrustumCullingThread.Start();
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsToBeCulled(Frustum frustum, BasePart part)
		{
			BoundingBox box = new BoundingBox(part.Position - part.Size / 2, part.Position + part.Size / 2);
			for (int i = 0; i < 6; i++)
			{
				Plane plane = frustum.Planes[i];
				Vector3 positiveVertex = box.Min;

				if (plane.Normal.X >= 0) positiveVertex.X = box.Max.X;
				if (plane.Normal.Y >= 0) positiveVertex.Y = box.Max.Y;
				if (plane.Normal.Z >= 0) positiveVertex.Z = box.Max.Z;

				if (plane.DistanceToPoint(positiveVertex) < 0)
					return true;
			}
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Frustum CaptureFrustum(Matrix4x4 viewProj)
		{
			Frustum frustum;
			frustum.Planes = new Plane[6];

			frustum.Planes[0].Normal.X = viewProj[0, 3] + viewProj[0, 0];
			frustum.Planes[0].Normal.Y = viewProj[1, 3] + viewProj[1, 0];
			frustum.Planes[0].Normal.Z = viewProj[2, 3] + viewProj[2, 0];
			frustum.Planes[0].Distance = viewProj[3, 3] + viewProj[3, 0];

			// Right plane
			frustum.Planes[1].Normal.X = viewProj[0, 3] - viewProj[0, 0];
			frustum.Planes[1].Normal.Y = viewProj[1, 3] - viewProj[1, 0];
			frustum.Planes[1].Normal.Z = viewProj[2, 3] - viewProj[2, 0];
			frustum.Planes[1].Distance = viewProj[3, 3] - viewProj[3, 0];

			// Bottom plane
			frustum.Planes[2].Normal.X = viewProj[0, 3] + viewProj[0, 1];
			frustum.Planes[2].Normal.Y = viewProj[1, 3] + viewProj[1, 1];
			frustum.Planes[2].Normal.Z = viewProj[2, 3] + viewProj[2, 1];
			frustum.Planes[2].Distance = viewProj[3, 3] + viewProj[3, 1];

			// Top plane
			frustum.Planes[3].Normal.X = viewProj[0, 3] - viewProj[0, 1];
			frustum.Planes[3].Normal.Y = viewProj[1, 3] - viewProj[1, 1];
			frustum.Planes[3].Normal.Z = viewProj[2, 3] - viewProj[2, 1];
			frustum.Planes[3].Distance = viewProj[3, 3] - viewProj[3, 1];

			// Near plane
			frustum.Planes[4].Normal.X = viewProj[0, 3] + viewProj[0, 2];
			frustum.Planes[4].Normal.Y = viewProj[1, 3] + viewProj[1, 2];
			frustum.Planes[4].Normal.Z = viewProj[2, 3] + viewProj[2, 2];
			frustum.Planes[4].Distance = viewProj[3, 3] + viewProj[3, 2];

			// Far plane
			frustum.Planes[5].Normal.X = viewProj[0, 3] - viewProj[0, 2];
			frustum.Planes[5].Normal.Y = viewProj[1, 3] - viewProj[1, 2];
			frustum.Planes[5].Normal.Z = viewProj[2, 3] - viewProj[2, 2];
			frustum.Planes[5].Distance = viewProj[3, 3] - viewProj[3, 2];

			// Normalize all planes
			for (int i = 0; i < 6; i++)
			{
				float length = frustum.Planes[i].Normal.Length();
				frustum.Planes[i].Normal /= length;
				frustum.Planes[i].Distance /= length;
			}
			return frustum;
		}

		public void AddParticle(Particle particle)
		{
			AllParticles.Add(particle);
		}
		public void RemoveParticle(Particle particle)
		{
			AllParticles.Remove(particle);
		}
		private void RenderParticles(float time)
		{
			Raylib.BeginBlendMode(BlendMode.Alpha);

			AllParticles.Sort((x, y) =>
			{
				float comp = (x.Position - MainCamera.Position).Length() - (y.Position - MainCamera.Position).Length();

				if (comp < 0) return 1;
				if (comp > 0) return -1;
				return 0;
			});

			for (int i = 0; i < AllParticles.Count; i++)
			{
				var particle = AllParticles[i];
				particle.Step(time);

				if (particle.LifetimeinSeconds >= particle.DeathLifetimeInSeconds)
				{
					RemoveParticle(particle);
					continue;
				}

				particle.Render(MainCamera);
			}

			Raylib.EndBlendMode();
		}
	}
	// ty chatgpt
	public struct Plane
	{
		public Vector3 Normal;
		public float Distance;

		public Plane()
		{
			Normal = new Vector3(0, 1, 0);
			Distance = 0;
		}
		public Plane(Vector3 normal, Vector3 position)
		{
			Normal = Vector3.Normalize(Normal);
			Distance = Vector3.Dot(Normal, position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceToPoint(Vector3 point) => Vector3.Dot(Normal, point) + Distance;
	}
	public ref struct Frustum
	{
		public Plane[] Planes;
	}
	[Flags]
	public enum Faces
	{
		Left = 1, Right = 2, Front = 4, Top = 8, Bottom = 16, Back = 32, All = Left | Right | Front | Top | Bottom | Back
	}
}
