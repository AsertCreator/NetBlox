using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Instances;
using System.Numerics;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	[Creatable]
	public class Character : Part
	{
		[Lua([Security.Capability.None])]
		[JsonIgnore]
		public bool IsLocalPlayer { get; set; }

		public Character() : base()
		{
			Color = Color.White;
			Anchored = false;
			Size = new Vector3(1, 1, 1);
		}

		public override void Render()
		{
			if (IsLocalPlayer && (NetworkManager.IsClient && !NetworkManager.IsServer))
			{
				if (Raylib.IsKeyPressed(KeyboardKey.G))
				{
					Part prt = new();

					prt.Name = "Trash";
					prt.Position = Position;
					prt.Size = Vector3.One;
					prt.TopSurface = SurfaceType.Studs;
					prt.Color = Color.DarkPurple;
					prt.Parent = this;
				}
			}

			base.Render();
		}
		public override void Process()
		{
			var cam = RenderManager.MainCamera;
			var x1 = cam.Position.X;
			var y1 = cam.Position.Z;
			var x2 = cam.Target.X;
			var y2 = cam.Target.Z;
			var angle = MathF.Atan2(y2 - y1, x2 - x1);

			if (IsLocalPlayer && (NetworkManager.IsClient && !NetworkManager.IsServer))
			{
				bool dot = false;

				if (Raylib.IsKeyDown(KeyboardKey.W))
				{
					Position = Position + new Vector3(0.2f * MathF.Cos(angle), 0, 0.2f * MathF.Sin(angle));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.A))
				{
					Position = Position + new Vector3(0.2f * MathF.Cos(angle - 1.5708f), 0, 0.2f * MathF.Sin(angle - 1.5708f));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.S))
				{
					Position = Position + new Vector3(-0.2f * MathF.Cos(angle), 0, -0.2f * MathF.Sin(angle));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.D))
				{
					Position = Position + new Vector3(-0.2f * MathF.Cos(angle - 1.5708f), 0, -0.2f * MathF.Sin(angle - 1.5708f));
					dot = true;
				}

				if (dot)
					ReplicateProps();
			}

			base.Process();
		}
		public override void RenderUI()
		{
			var cam = RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(Position + new Vector3(0, Size.Y / 2 + 1f, 0), cam);
			var siz = Raylib.MeasureTextEx(RenderManager.MainFont, Name, 14, 1.4f);

			Raylib.DrawTextEx(RenderManager.MainFont, Name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Character) == classname) return true;
			return base.IsA(classname);
		}
	}
}
