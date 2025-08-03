using NetBlox.Runtime;
using NetBlox.Common;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;
using NetBlox.Instances.Services;
using NetBlox.Network;

namespace NetBlox.Instances
{
	public enum HumanoidState
	{
		Idle, Falling, Sitting, Walking, Jumping, Swimming, FrozenFalling, Dead
	}
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

		public BasePart[] AllBodyParts => Parent.Children.Where(x => x is BasePart).Cast<BasePart>().ToArray(); // wtf
		public BasePart? PrimaryPart => Parent is Model ? (Parent as Model).PrimaryPart : null;
		public BasePart? Head => Parent.FindFirstChild("Head") as BasePart;
		public BasePart? RightLeg => Parent.FindFirstChild("Right Leg") as BasePart;
		public BasePart? LeftLeg => Parent.FindFirstChild("Left Leg") as BasePart;
		public BasePart? RightArm => Parent.FindFirstChild("Right Arm") as BasePart;
		public BasePart? LeftArm => Parent.FindFirstChild("Left Arm") as BasePart;
		public bool IsRightLegAbleToJump => RightLeg.IsGrounded;
		public bool IsLeftLegAbleToJump => LeftLeg.IsGrounded;
		public bool CanJumpInTheory => IsRightLegAbleToJump || IsLeftLegAbleToJump;

		public static HumanoidControl ControlForward = HumanoidControl.GetFor(HumanoidControlType.Forward);
		public static HumanoidControl ControlBackward = HumanoidControl.GetFor(HumanoidControlType.Backward);
		public static HumanoidControl ControlLeft = HumanoidControl.GetFor(HumanoidControlType.WalkLeft);
		public static HumanoidControl ControlRight = HumanoidControl.GetFor(HumanoidControlType.WalkRight);
		public static HumanoidControl ControlJump = HumanoidControl.GetFor(HumanoidControlType.Jump);

		public HumanoidState State = HumanoidState.Idle;
		private Workspace? workspace;
		private bool isDying = false;
		private BasePart? torsoCache;

		public Humanoid(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Humanoid) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void ResetCharacter()
		{
			GameManager.NetworkManager.SendServerboundPacket(NPCharacterReset.Create(Parent));
		}
		public override void Process()
		{
			base.Process();

			if (Parent == null) return;
			if (torsoCache == null)
				torsoCache = Parent.FindFirstChild("Torso") as BasePart;

			if (GameManager.NetworkManager == null) return;

			if (IsLocalPlayer && GameManager.NetworkManager.IsClient && Health > 0)
			{
				ProcessInput();
			}

			if (Health <= 0 && IsLocalPlayer && !isDying)
			{
				isDying = true;
				Die();
			}

			Health = Math.Max(Health, 0);
		}
		private void ProcessInput()
		{
			var primary = PrimaryPart;
			if (primary == null)
				return;

			if (!CanJumpInTheory && State != HumanoidState.Falling)
				State = HumanoidState.Falling;
			if (Health <= 0)
				State = HumanoidState.Dead;
			if (State != HumanoidState.FrozenFalling && primary.LinearVelocity.Length() < 3)
				State = HumanoidState.Idle;

			if (workspace == null)
			{
				workspace = Root.GetService<Workspace>(true);
				if (workspace == null) // no humanoids may function without a workspace
					return;
			}

			switch (State)
			{
				case HumanoidState.Idle:
					// torsoCache.Rotation = default;
					DoWalking();
					break;
				case HumanoidState.Falling:
					// torsoCache.Rotation = default;
					DoFalling();
					DoWalking();
					break;
				case HumanoidState.Sitting:
					DoSitting();
					break;
				case HumanoidState.Walking:
					DoWalking();
					break;
				case HumanoidState.Jumping:
					DoWalking();
					break;
				case HumanoidState.Swimming:
					break;
				case HumanoidState.FrozenFalling:
					DoSitting();
					break;
				case HumanoidState.Dead:
					Health = 0;
					if (!isDying)
					{
						Die();
						isDying = true;
					}
					break;
			}
		}
		private void DoWalking()
		{
			bool ismovingforward = ControlForward.IsPressed();
			bool ismovingbackward = ControlBackward.IsPressed();
			bool ismovingsideleft = ControlLeft.IsPressed();
			bool ismovingsideright = ControlRight.IsPressed();

			var camera = GameManager.RenderManager.MainCamera;
			float x1 = camera.Position.X;
			float y1 = camera.Position.Z;
			float x2 = camera.Target.X;
			float y2 = camera.Target.Z;
			float angle = MathF.Atan2(y2 - y1, x2 - x1);
			float deltatime = (float)TaskScheduler.LastCycleTime.TotalSeconds;

			Vector3 veldelta = default;

			if (ControlJump.IsPressed() && State != HumanoidState.Falling)
				StandUp();

			if (ismovingforward)
				veldelta += new Vector3(
					WalkSpeed * MathF.Cos(angle) * deltatime, 0, WalkSpeed * MathF.Sin(angle) * deltatime);
			if (ismovingsideleft)
				veldelta += new Vector3(
					WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
			if (ismovingbackward)
				veldelta += new Vector3(
					-WalkSpeed * MathF.Cos(angle) * deltatime, 0, -WalkSpeed * MathF.Sin(angle) * deltatime);
			if (ismovingsideright)
				veldelta += new Vector3(
					-WalkSpeed * MathF.Cos(angle - 1.5708f) * deltatime, 0, -WalkSpeed * MathF.Sin(angle - 1.5708f) * deltatime);
			veldelta = Vector3.Normalize(veldelta) * WalkSpeed;

			if (ismovingbackward || ismovingforward || ismovingsideleft || ismovingsideright)
			{
				if (State != HumanoidState.Falling)
				{
					State = HumanoidState.Walking;
				}
				PrimaryPart.Velocity = new Vector3((PrimaryPart.LinearVelocity.X + veldelta.X) / 2,
					PrimaryPart.LinearVelocity.Y, (PrimaryPart.LinearVelocity.Z + veldelta.Z) / 2);
			}
		}
		private void DoSitting()
		{
			if (ControlJump.IsPressed())
				StandUp();
		}
		private void DoFalling()
		{
			if (CanJumpInTheory || ControlJump.IsPressed())
				StandUp();
		}
		private void StandUp()
		{
			var parts = AllBodyParts;
			for (int i = 0; i < parts.Length; i++)
			{
				var part = parts[i];
				part.Velocity += new Vector3(0, 5, 0);
				if (part.Velocity.Y >= 7)
					part.Velocity = new Vector3(part.Velocity.X, 7, part.Velocity.Z);
				part.RenderPositionOffset = default;
				part.RenderRotationOffset = Quaternion.Identity;
			}

			State = HumanoidState.Idle;
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

			var name = Parent.Name + " - " + State;

			siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont.SpriteFont, name, 14, 1.4f);
			Raylib.DrawTextEx(GameManager.RenderManager.MainFont.SpriteFont, name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);

			if (Health < 100)
			{
				siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont.SpriteFont, Health.ToString(), 14, 1.4f);
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont.SpriteFont, Health.ToString(), pos - new Vector2(siz.X / 2, -16), 14, 1.4f,
					new Color(255,
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						255));
			}
		}
		public void Die()
		{
			if (GameManager.NetworkManager.IsServer)
			{
				(Parent as Model)?.BreakJoints();
				Task.Delay(4000).ContinueWith(_ =>
				{
					TaskScheduler.Schedule(() =>
					{

					});
				});
			}
		}
	}
}
