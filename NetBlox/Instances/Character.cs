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
			Anchored = true;
			Size = new Vector3(1, 1, 1);

			// yes. unnecessary calculations. but what then?
			Scene sc = Root.GetService<Workspace>().Scene;
			lock (sc)
			{
				sc.RemoveBody(Body);
			}
			Body = null;
			Box = null;
			BoxDef = null;
			GameManager.PhysicsManager.Actors.Remove(this);
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
					prt.Color3 = Color.DarkPurple;
					prt.Parent = this;
				}
			}

			base.Render();
		}
		public override void Process()
		{
			if (GameManager.RenderManager == null) return;
			var cam = GameManager.RenderManager.MainCamera;
			var x1 = cam.Position.X;
			var y1 = cam.Position.Z;
			var x2 = cam.Target.X;
			var y2 = cam.Target.Z;
			var angle = MathF.Atan2(y2 - y1, x2 - x1);

			if (IsLocalPlayer && (GameManager.NetworkManager.IsClient && !GameManager.NetworkManager.IsServer) && Health > 0)
			{
				bool poschg = false;
				bool rotchg = false;

				Vector3 posdelta = Vector3.Zero;
				Vector3 rotdelta = Vector3.Zero;
				float deltatime = (float)TaskScheduler.LastCycleTime.TotalSeconds;

				bool movingonangle = true;

				if (Raylib.IsKeyDown(KeyboardKey.Space))
				{
					posdelta += new Vector3(0, 0.7f * WalkSpeed * deltatime, 0);
					poschg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
				{
					posdelta += new Vector3(0, -WalkSpeed * deltatime, 0);
					poschg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.Q))
				{
					rotdelta += new Vector3(0, WalkSpeed, 0);
					rotchg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.E))
				{
					rotdelta += new Vector3(0, -WalkSpeed, 0);
					rotchg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.W))
				{
					posdelta += new Vector3(WalkSpeed * MathF.Cos(angle) * deltatime, 0, WalkSpeed * MathF.Sin(angle) * deltatime);
					movingonangle = !movingonangle;
					poschg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.A))
				{
					posdelta += new Vector3(WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
					movingonangle = !movingonangle;
					poschg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.S))
				{
					posdelta += new Vector3(-WalkSpeed * MathF.Cos(angle) * deltatime, 0, -WalkSpeed * MathF.Sin(angle) * deltatime);
					movingonangle = !movingonangle;
					poschg = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.D))
				{
					posdelta += new Vector3(-WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, -WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
					movingonangle = !movingonangle;
					poschg = true;
				}

				posdelta = posdelta != Vector3.Zero ? Vector3.Normalize(posdelta) : posdelta;

				Position += posdelta * 0.22f;
				Rotation += rotdelta * 0.22f;

				if (poschg && !rotchg)
					ReplicateProperties(["Position"], false);
				if (!poschg && rotchg)
					ReplicateProperties(["Rotation"], false);
				if (poschg && rotchg)
					ReplicateProperties(["Position", "Rotation"], false);
			}

			if (Health <= 0 && IsLocalPlayer && !isDying)
			{
				isDying = true;
				Die();
			}

			Health = Math.Max(Health, 0);

			base.Process();
		}
		public override void RenderUI()
		{
			var cam = GameManager.RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(Position + new Vector3(0, Size.Y / 2 + 1f, 0), cam);
			var siz = Vector2.Zero; 

			siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Name, 14, 1.4f);
			Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);

			if (Health < 100)
			{
				siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, Health.ToString(), 14, 1.4f);
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont, Health.ToString(), pos - new Vector2(siz.X / 2, -16), 14, 1.4f, 
					new Color(255,
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						255));
			}
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
