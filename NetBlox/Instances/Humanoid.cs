using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Common;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Humanoid : Instance
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

		public Humanoid(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Humanoid) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
			if (Parent == null) return;

			var parent = Parent;
			var model = parent as Model;
			if (model == null) return;
			var head = model.FindFirstChild("Head");
			if (head == null)
				Health = 0;

			if (GameManager.RenderManager == null) return;
			var cam = GameManager.RenderManager.MainCamera;
			var x1 = cam.Position.X;
			var y1 = cam.Position.Z;
			var x2 = cam.Target.X;
			var y2 = cam.Target.Z;
			var angle = MathF.Atan2(y2 - y1, x2 - x1);

			if (GameManager.NetworkManager == null) return;

			if (IsLocalPlayer && (GameManager.NetworkManager.IsClient && !GameManager.NetworkManager.IsServer) && Health > 0)
			{
				bool dot = false;

				if (Raylib.IsKeyDown(KeyboardKey.W))
				{
					model.SetPivot(model.GetPivot() * new CFrame(new(0.1f * WalkSpeed / 12 * MathF.Cos(angle), 0, 0.1f * WalkSpeed / 12 * MathF.Sin(angle))));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.A))
				{
					model.SetPivot(model.GetPivot() * new CFrame(new(0.1f * WalkSpeed / 12 * MathF.Cos(angle - 1.5708f), 0, 0.1f * WalkSpeed / 12 * MathF.Sin(angle - 1.5708f))));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.S))
				{
					model.SetPivot(model.GetPivot() * new CFrame(new(-0.1f * WalkSpeed / 12 * MathF.Cos(angle), 0, -0.1f * WalkSpeed / 12 * MathF.Sin(angle))));
					dot = true;
				}
				if (Raylib.IsKeyDown(KeyboardKey.D))
				{
					model.SetPivot(model.GetPivot() * new CFrame(new(-0.1f * WalkSpeed / 12 * MathF.Cos(angle - 1.5708f), 0, -0.1f * WalkSpeed / 12 * MathF.Sin(angle - 1.5708f))));
					dot = true;
				}

				if (dot)
					ReplicateProperties(["Position", "Rotation"], false);
			}

			if (Health <= 0 && IsLocalPlayer && !isDying)
			{
				isDying = true;
				Die();
			}

			Health = Math.Max(Health, 0);
		}
		public override void RenderUI()
		{
			if (Parent == null) return;

			var parent = Parent;
			if (parent is not Model)
			{
				LogManager.LogWarn("Humanoid instance must be parented to a Model!");
				return;
			}
			if (GameManager.RenderManager == null) return;

			var head = parent.FindFirstChild("Head") as BasePart;
			if (head == null) return;

			var cam = GameManager.RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(head.Position + new Vector3(0, head.Size.Y / 2 + 1f, 0), cam);
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
			Task.Delay(4000).ContinueWith(_ =>
			{
				((Player)Root.GetService<Players>().LocalPlayer!).LoadCharacterOld();
			});
		}
	}
}
