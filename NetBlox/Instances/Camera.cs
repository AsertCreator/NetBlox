using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Camera : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? CameraSubject
		{
			get => FormalSubject;
			set
			{
				if (value is BasePart bp)
				{
					ActualSubject = bp;
					FormalSubject = value;
				}
				else if (value is Model model)
				{
					var pp = model.GetPivotPart();
					if (pp == null)
						return;
					ActualSubject = pp;
					FormalSubject = value;
				}
				else if (value is Humanoid hum)
				{
					var head = hum.Parent.FindFirstChild("Head");
					if (head == null)
						return;
					var bphead = head as BasePart;
					ActualSubject = bphead;
					FormalSubject = value;
				}
				else
				{
					LogManager.LogWarn("Cannot set the camera to look at non-BasePart, non-Model or non-Humanoid Instances!");
					FormalSubject = null;
					ActualSubject = null;
					return;
				}
			}
		}
		public static Vector2 LastMousePosition;
		private Instance? FormalSubject;
		private BasePart? ActualSubject;

		public Camera(GameManager ins) : base(ins) { }

		public override void Process()
		{
			if (GameManager.NetworkManager.IsServer)
				return; // im sick of it
			if (CameraSubject == null)
			{
				GameManager.RenderManager.MainCamera.Position = new Vector3(0, 5, -6);
				GameManager.RenderManager.MainCamera.Target = Vector3.Zero;
			}
			else
			{
				Vector3 subjectposition = Vector3.One;
				if (ActualSubject != null)
					subjectposition = ActualSubject.Position;

				var player = Root.GetService<Players>().LocalPlayer as Player;

				if (player == null) return; // nah

				if (GameManager.RenderManager.FocusedBox == null)
				{
					// Camera rotation
					if (Raylib.IsKeyDown(KeyboardKey.Down)) Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, 0.03f, true, true, false);
					if (Raylib.IsKeyDown(KeyboardKey.Up)) Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, -0.03f, true, true, false);
					if (Raylib.IsKeyDown(KeyboardKey.Right)) Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, 0.03f, true);
					if (Raylib.IsKeyDown(KeyboardKey.Left)) Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, -0.03f, true);

					if (Raylib.IsMouseButtonDown(MouseButton.Right))
					{
						Vector2 mousePositionDelta = Raylib.GetMousePosition() - LastMousePosition;

						// Mouse support
						Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, -mousePositionDelta.X * 0.003f, true);
						Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, -mousePositionDelta.Y * 0.003f, true, true, false);

						Raylib.SetMousePosition((int)LastMousePosition.X, (int)LastMousePosition.Y);
					}

					// Zoom target distance

					float move = -Raylib.GetMouseWheelMove();
					if (move > 0)
					{
						if ((GameManager.RenderManager.MainCamera.Position - GameManager.RenderManager.MainCamera.Target)
							.Length() < (player.CameraMaxZoomDistance - 0.2f))
							Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, move);
					}
					else
					{
						if ((GameManager.RenderManager.MainCamera.Position - GameManager.RenderManager.MainCamera.Target)
							.Length() > (player.CameraMinZoomDistance + 0.2f))
							Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, move);
					}

					if (Raylib.IsKeyDown(KeyboardKey.O))
					{
						if ((GameManager.RenderManager.MainCamera.Position - GameManager.RenderManager.MainCamera.Target)
							.Length() < player.CameraMaxZoomDistance)
							Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, 0.2f);
					}
					if (Raylib.IsKeyDown(KeyboardKey.I))
					{
						if ((GameManager.RenderManager.MainCamera.Position - GameManager.RenderManager.MainCamera.Target)
							.Length() > player.CameraMinZoomDistance)
							Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, -0.2f);
					}
				}

				var diff = GameManager.RenderManager.MainCamera.Target - GameManager.RenderManager.MainCamera.Position;

				GameManager.RenderManager.MainCamera.Position = ActualSubject.Position - diff;
				GameManager.RenderManager.MainCamera.Target = ActualSubject.Position;
				LastMousePosition = Raylib.GetMousePosition();
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Camera) == classname) return true;
			return base.IsA(classname);
		}
	}
}
