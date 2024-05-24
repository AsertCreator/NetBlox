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
		[NotReplicated]
		public bool IsLocalPlayer { get; set; }

		public Character(GameManager gm) : base(gm)
		{
			var c = Color.White;
			c.A = 255;
			Color = c;
			Anchored = false;
			Size = new Vector3(1, 1, 1);
		}

		public override void Render()
		{
			if (IsLocalPlayer && (GameManager.NetworkManager.IsClient && !GameManager.NetworkManager.IsServer))
			{
				if (Raylib.IsKeyPressed(KeyboardKey.G))
				{
					Part prt = new(GameManager);

					prt.Name = "Trash";
					prt.Position = Position;
					prt.Size = Vector3.One;
					prt.TopSurface = SurfaceType.Studs;
					prt.Color = Color.DarkPurple;
					prt.Parent = this;
				}
			}

			GameManager.RenderManager.SetLight(0, 1, Position, Vector3.Zero, Color);
			base.Render();
		}
		public override void Process()
		{
			var cam = GameManager.RenderManager.MainCamera;
			var x1 = cam.Position.X;
			var y1 = cam.Position.Z;
			var x2 = cam.Target.X;
			var y2 = cam.Target.Z;
			var angle = MathF.Atan2(y2 - y1, x2 - x1);

			if (IsLocalPlayer && (GameManager.NetworkManager.IsClient && !GameManager.NetworkManager.IsServer))
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
			var cam = GameManager.RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(Position + new Vector3(0, Size.Y / 2 + 1f, 0), cam);
			var siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Name, 14, 1.4f);

			Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Character) == classname) return true;
			return base.IsA(classname);
		}
	}
}
