using Raylib_cs;
using NetBlox;
using NetBlox.Instances;
using NetBlox.Runtime;
using System.Numerics;

namespace NetBlox.Instances
{
	public class Camera : Instance
	{
		[Lua]
		[Replicated]
		public Instance? CameraSubject { get; set; }
		public static Vector2 LastMousePosition;

		public override void Process()
		{
			if (CameraSubject == null || !CameraSubject.IsA("BasePart"))
			{
				RenderManager.MainCamera.Position = new Vector3(50, 40, 0);
				RenderManager.MainCamera.Target = Vector3.Zero;
			}
			else
			{
				var subject = CameraSubject as BasePart ?? throw new Exception("CameraSubject is not BasePart");

				// Camera rotation
				if (Raylib.IsKeyDown(KeyboardKey.Down)) Raylib.CameraPitch(ref RenderManager.MainCamera, 0.03f, true, true, false);
				if (Raylib.IsKeyDown(KeyboardKey.Up)) Raylib.CameraPitch(ref RenderManager.MainCamera, -0.03f, true, true, false);
				if (Raylib.IsKeyDown(KeyboardKey.Right)) Raylib.CameraYaw(ref RenderManager.MainCamera, 0.03f, true);
				if (Raylib.IsKeyDown(KeyboardKey.Left)) Raylib.CameraYaw(ref RenderManager.MainCamera, -0.03f, true);

				if (Raylib.IsMouseButtonDown(MouseButton.Right))
				{
					Vector2 mousePositionDelta = Raylib.GetMouseDelta();

					// Mouse support
					Raylib.CameraYaw(ref RenderManager.MainCamera, -mousePositionDelta.X * 0.003f, true);
					Raylib.CameraPitch(ref RenderManager.MainCamera, -mousePositionDelta.Y * 0.003f, true, true, false);
				}

				// Zoom target distance
				Raylib.CameraMoveToTarget(ref RenderManager.MainCamera, -Raylib.GetMouseWheelMove());
				if (Raylib.IsKeyDown(KeyboardKey.O)) Raylib.CameraMoveToTarget(ref RenderManager.MainCamera, 0.2f);
				if (Raylib.IsKeyDown(KeyboardKey.I)) Raylib.CameraMoveToTarget(ref RenderManager.MainCamera, -0.2f);

				var diff = RenderManager.MainCamera.Target - RenderManager.MainCamera.Position;

				RenderManager.MainCamera.Position = subject.Position - diff;
				RenderManager.MainCamera.Target = subject.Position;
			}
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(Camera) == classname) return true;
			return base.IsA(classname);
		}
	}
}
