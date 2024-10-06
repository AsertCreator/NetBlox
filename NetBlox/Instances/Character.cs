using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Instances;
using System.Numerics;
using System.Text.Json.Serialization;
using NetBlox.Instances.Services;
using NetBlox.Common;
using Qu3e;

namespace NetBlox.Instances
{
	[Creatable]
	public class Character : Part
	{
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public bool IsLocalPlayer { get; set; }
		[Lua([Security.Capability.None])]
		public float Health { get; set; } = 100;
		[Lua([Security.Capability.None])]
		public float WalkSpeed { get; set; } = 12;
		[Lua([Security.Capability.None])]
		public float JumpPower { get; set; } = 6;
		private bool isDying = false;

		public Character(GameManager gm) : base(gm)
		{
			var c = Color.White;
			c.A = 255;
			Color3 = c;
			Locked = true;
			Size = new Vector3(2, 2, 2);
		}
		public override void Process()
		{
			base.Process();

			if (GameManager.RenderManager == null) return;

			if (IsLocalPlayer && GameManager.NetworkManager.IsClient && Health > 0)
			{
				var cam = GameManager.RenderManager.MainCamera;
				var x1 = cam.Position.X;
				var y1 = cam.Position.Z;
				var x2 = cam.Target.X;
				var y2 = cam.Target.Z;
				var angle = MathF.Atan2(y2 - y1, x2 - x1);
				float deltatime = (float)TaskScheduler.LastCycleTime.TotalSeconds;
				Vector3 veldelta = Vector3.Zero;
				Vector3 rotdelta = Vector3.Zero;

				if (Body == null)
					return;

				if (GameManager.RenderManager.FocusedBox == null)
				{
					if (Raylib.IsKeyDown(KeyboardKey.Space) && (Body.ContactList.Count != 0 || GameManager.PhysicsManager.DisablePhysics))
						veldelta += new Vector3(0, JumpPower * deltatime, 0);
					if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
						veldelta += new Vector3(0, -WalkSpeed * deltatime, 0);
					if (Raylib.IsKeyDown(KeyboardKey.Q))
						rotdelta += new Vector3(0, WalkSpeed, 0);
					if (Raylib.IsKeyDown(KeyboardKey.E))
						rotdelta += new Vector3(0, -WalkSpeed, 0);
					if (Raylib.IsKeyDown(KeyboardKey.W))
						veldelta += new Vector3(
							WalkSpeed * MathF.Cos(angle) * deltatime, 0, WalkSpeed * MathF.Sin(angle) * deltatime);
					if (Raylib.IsKeyDown(KeyboardKey.A))
						veldelta += new Vector3(
							WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
					if (Raylib.IsKeyDown(KeyboardKey.S))
						veldelta += new Vector3(
							-WalkSpeed * MathF.Cos(angle) * deltatime, 0, -WalkSpeed * MathF.Sin(angle) * deltatime);
					if (Raylib.IsKeyDown(KeyboardKey.D))
						veldelta += new Vector3(
							-WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, -WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
					if (Raylib.IsKeyPressed(KeyboardKey.G))
					{
						Part prt = new(GameManager);

						prt.Name = "Trash";
						prt.Position = Position;
						prt.Size = Vector3.One;
						prt.TopSurface = SurfaceType.Studs;
						prt.Color3 = Color.DarkPurple;
						prt.Parent = this;
					}
				}

				veldelta = veldelta != Vector3.Zero ? Vector3.Normalize(veldelta) : veldelta;

				if (!GameManager.PhysicsManager.DisablePhysics)
					Velocity += veldelta * 0.38f;
				else
					Position += veldelta * 0.22f;
				Rotation += rotdelta * 0.22f;

				if (Health <= 0 && IsLocalPlayer && !isDying)
				{
					isDying = true;
					Die();
				}

				Health = Math.Max(Health, 0);
			}
		}
		public override void RenderUI()
		{
			var cam = GameManager.RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(Position + new Vector3(0, Size.Y / 2 + 1f, 0), cam);
			var siz = Vector2.Zero;
			var font = GameManager.RenderManager.MainFont;

			siz = Raylib.MeasureTextEx(font, Name, 14, 1.4f);
			Raylib.DrawTextEx(font, Name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);

			if (Health < 100)
			{
				siz = Raylib.MeasureTextEx(font, Health.ToString(), 14, 1.4f);
				Raylib.DrawTextEx(font, Health.ToString(), pos - new Vector2(siz.X / 2, -16), 14, 1.4f, 
					new Color(255,
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						255));
			}
			Raylib.DrawTextEx(font, $"Position: {Position.X}, {Position.Y}, {Position.Z}", new Vector2(100, 100), 14, 1.4f, Color.White);
			Raylib.DrawTextEx(font, $"Rotation: {Rotation.X}, {Rotation.Y}, {Rotation.Z}", new Vector2(100, 116), 14, 1.4f, Color.White);
			Raylib.DrawTextEx(font, $"Velocity: {Velocity.X}, {Velocity.Y}, {Velocity.Z}", new Vector2(100, 132), 14, 1.4f, Color.White);
		}
		public void Die()
		{
			LogManager.LogInfo("Character had died!");
			Root.GetService<Debris>().AddItem(this, 4);
			RenderManager.LoadSound("rbxasset://sounds/grunt.mp3", GameManager.RenderManager.PlaySound);
			Task.Delay(4000).ContinueWith(_ =>
			{
				((Player)Root.GetService<Players>().LocalPlayer!).LoadCharacterOld();
			});
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Character) == classname) return true;
			return base.IsA(classname);
		}
	}
}
