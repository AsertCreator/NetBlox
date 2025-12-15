using NetBlox.Common;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using Raylib_cs;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

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
		public override Instance? Parent 
		{
			get => base.Parent;
			set 
			{
				if (value is not Model)
				{
					LogManager.LogWarn("Humanoid instance must be parented to a Model!");
					return;
				}

				base.Parent = value;

				SetupBodyPartsAutomation();
			}
		}

		public BasePart[] AllBodyParts => Parent.Children.Where(x => x is BasePart).Cast<BasePart>().ToArray(); // wtf
		public BasePart? PrimaryPart => Parent is Model ? (Parent as Model).PrimaryPart : null;
		public BasePart? Head => head;
		public BasePart? RightLeg => rightLeg;
		public BasePart? LeftLeg => leftLeg;
		public BasePart? RightArm => rightArm;
		public BasePart? LeftArm => leftArm;
		public bool IsRightLegAbleToJump => RightLeg == null ? false : RightLeg.IsGrounded;
		public bool IsLeftLegAbleToJump => LeftLeg == null ? false : LeftLeg.IsGrounded;
		public bool CanJumpInTheory => IsRightLegAbleToJump || IsLeftLegAbleToJump;

		public static HumanoidControl ControlForward = HumanoidControl.GetFor(HumanoidControlType.Forward);
		public static HumanoidControl ControlBackward = HumanoidControl.GetFor(HumanoidControlType.Backward);
		public static HumanoidControl ControlLeft = HumanoidControl.GetFor(HumanoidControlType.WalkLeft);
		public static HumanoidControl ControlRight = HumanoidControl.GetFor(HumanoidControlType.WalkRight);
		public static HumanoidControl ControlJump = HumanoidControl.GetFor(HumanoidControlType.Jump);

		public HumanoidState State
		{
			get => currentState;
			set
			{
				var old = currentState;
				currentState = value;
				var newv = value;
				if (old != newv)
					DoStateTransition(old, newv);
			}
		}

		private HumanoidState currentState = HumanoidState.Idle;
		private Workspace? workspace;
		private bool isDying = false;
		private BasePart? torsoCache;
		private BasePart? leftLeg;
		private BasePart? rightLeg;
		private BasePart? leftArm;
		private BasePart? rightArm;
		private BasePart? head;
		private RenderTexture2D debugTexture;

		public Humanoid(GameManager ins) : base(ins) { }
		
		[Lua([Security.Capability.CoreSecurity])]
		public void ResetCharacter()
		{
			GameManager.NetworkManager.SendServerboundPacket(NPCharacterReset.Create(Parent));
		}
		private void RenewBodyPartsEventHandler(object sender, Instance descendant) => RenewBodyParts();
		public void RenewBodyParts()
		{
			torsoCache = Parent.FindFirstChild("Torso") as BasePart;
			leftLeg = Parent.FindFirstChild("Left Leg") as BasePart;
			rightLeg = Parent.FindFirstChild("Right Leg") as BasePart;
			leftArm = Parent.FindFirstChild("Left Arm") as BasePart;
			rightArm = Parent.FindFirstChild("Right Arm") as BasePart;
		}
		public void SetupBodyPartsAutomation()
		{
			RenewBodyParts();
			Parent.NativeChildAdded += RenewBodyPartsEventHandler;
			Parent.NativeChildRemoved += RenewBodyPartsEventHandler;
		}
		public override void Process()
		{
			base.Process();

			if (Parent == null) return;
			if (GameManager.NetworkManager == null) return;

			if (IsLocalPlayer && GameManager.NetworkManager.IsClient && Health > 0)
			{
				ProcessInput();
			}

			if (Health <= 0 && !IsLocalPlayer && !isDying)
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

			if (!CanJumpInTheory && State != HumanoidState.Falling && State != HumanoidState.FrozenFalling && 
				MathF.Abs(torsoCache.Velocity.Y) > 2)
				State = HumanoidState.Falling;
			if (MathF.Abs(torsoCache.Velocity.Y) < 2)
				State = HumanoidState.Idle;
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
					StabilizeHumanoid();
					DoWalking();
					break;
				case HumanoidState.Falling:
					StabilizeHumanoid();
					DoFalling();
					DoWalking();
					break;
				case HumanoidState.Sitting:
					DoSitting();
					break;
				case HumanoidState.Walking:
					StabilizeHumanoid();
					DoWalking();
					break;
				case HumanoidState.Jumping:
					StabilizeHumanoid();
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
				veldelta += new Vector3(MathF.Cos(angle) * deltatime, 0, MathF.Sin(angle) * deltatime);
			if (ismovingsideleft)
				veldelta += new Vector3(MathF.Cos(angle - 1.5708f) * deltatime, 0, MathF.Sin(angle - 1.5708f) * deltatime);
			if (ismovingbackward)
				veldelta += new Vector3(-MathF.Cos(angle) * deltatime, 0, -MathF.Sin(angle) * deltatime);
			if (ismovingsideright)
				veldelta += new Vector3(-MathF.Cos(angle - 1.5708f) * deltatime, 0, -MathF.Sin(angle - 1.5708f) * deltatime);

			veldelta = Vector3.Normalize(veldelta) * WalkSpeed;

			if (ismovingbackward || ismovingforward || ismovingsideleft || ismovingsideright)
			{
				if (State != HumanoidState.Falling)
				{
					State = HumanoidState.Walking;
				}
				torsoCache.Velocity = new Vector3(MathE.Lerp(torsoCache.LinearVelocity.X, veldelta.X, 0.7f),
					torsoCache.LinearVelocity.Y, MathE.Lerp(torsoCache.LinearVelocity.Z, veldelta.Z, 0.7f));

				if (torsoCache.BodyHandle.HasValue)
				{
					Vector3 characterForward = Raymath.Vector3RotateByQuaternion(Vector3.UnitZ, torsoCache.QuaternionRotation);
					Vector3 cameraForward = GameManager.RenderManager.MainCamera.Target - GameManager.RenderManager.MainCamera.Position;
					cameraForward.Y = 0;
					cameraForward = Vector3.Normalize(cameraForward);
					characterForward.Y = 0;
					characterForward = Vector3.Normalize(characterForward);

					float anglediff = MathF.Atan2(
						cameraForward.X * characterForward.Z - cameraForward.Z * characterForward.X,
						cameraForward.X * characterForward.X + cameraForward.Z * characterForward.Z);
					float angular_velocity = MathE.Clamp(-5, anglediff, 5);

					var angular = torsoCache.AngularVelocity;
					angular.Y = angular_velocity * 3;
					var body = GameManager.PhysicsManager.LocalSimulation.Bodies[torsoCache.BodyHandle.Value];
					body.ApplyAngularImpulse(angular);
					body.Awake = true;
				}
			}
			else
			{
				torsoCache.Velocity = new Vector3(torsoCache.LinearVelocity.X / 8,
					torsoCache.LinearVelocity.Y, torsoCache.LinearVelocity.Z / 8);
			}
		}
		private void StabilizeHumanoid()
		{
			if (torsoCache.BodyHandle.HasValue)
			{
				// may chatgpt help me
				Vector3 up = Raymath.Vector3RotateByQuaternion(Vector3.UnitY, torsoCache.QuaternionRotation);
				Vector3 correctionAxis = Raymath.Vector3CrossProduct(up, Vector3.UnitY);
				float angleError = MathF.Acos(Raymath.Vector3DotProduct(up, Vector3.UnitY));
				var torque = Raymath.Vector3Normalize(correctionAxis) * (angleError * 240) - torsoCache.AngularVelocity * 1f;
				if (float.IsNaN(torque.X) || float.IsNaN(torque.Y) || float.IsNaN(torque.Z))
					return;
				if (float.IsInfinity(torque.X) || float.IsInfinity(torque.Y) || float.IsInfinity(torque.Z))
					return;
				torsoCache.AngularVelocity += torque;
				torsoCache.Velocity += new Vector3(0, 0.1f, 0);
			}
		}
		private void DoStateTransition(HumanoidState from, HumanoidState to)
		{

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
			var part = torsoCache;
			part.Velocity += new Vector3(0, 35, 0);
			if (part.Velocity.Y >= 38)
				part.Velocity = new Vector3(part.Velocity.X, 38, part.Velocity.Z);

			State = HumanoidState.Jumping;
		}
		public override void Destroy()
		{
			Parent.NativeChildAdded -= RenewBodyPartsEventHandler;
			Parent.NativeChildRemoved -= RenewBodyPartsEventHandler;
			base.Destroy();
		}
		public override void RenderUI()
		{
			if (Parent == null) return;

			var parent = Parent;
			if (parent is not Model)
				return;

			if (GameManager.RenderManager == null) return;

			var head = parent.FindFirstChild("Head") as BasePart;
			if (head == null) return;

			var cam = GameManager.RenderManager.MainCamera;
			var pos = Raylib.GetWorldToScreen(head.Position + new Vector3(0, head.Size.Y / 2 + 1f, 0), cam);
			var siz = Vector2.Zero;

			var name = Parent.Name;

			siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont14.SpriteFont, name, 14, 1.4f);
			Raylib.DrawTextEx(GameManager.RenderManager.MainFont14.SpriteFont, name, pos - new Vector2(siz.X / 2, 0), 14, 1.4f, Color.White);

			if (Health < 100)
			{
				siz = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont14.SpriteFont, Health.ToString(), 14, 1.4f);
				Raylib.DrawTextEx(GameManager.RenderManager.MainFont14.SpriteFont, Health.ToString(), pos - new Vector2(siz.X / 2, -16), 14, 1.4f, 
					new Color(255,
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						(int)MathE.Lerp(0, 255, Math.Clamp(Health, 0, 100) / 100f),
						255));
			}

			RenderDebugHud();
		}
		public void RenderDebugHud()
		{
			var head = Parent.FindFirstChild("Head") as BasePart;
			if (head == null) return;

			const int windowWidth = 350;
			const int windowHeight = 150;

			var cam = GameManager.RenderManager.MainCamera;
			var debugbillboardpos = Raylib.GetWorldToScreen(head.Position + new Vector3(0, head.Size.Y / 2 + 2f, 0), cam);
			var scale = 10 / Vector3.Distance(cam.Position, head.Position + new Vector3(0, head.Size.Y / 2 + 2f, 0));
			var font = GameManager.RenderManager.MainFont14.SpriteFont;

			debugbillboardpos += new Vector2(-windowWidth / 2, -windowHeight / 1.2f);

			if (debugTexture.Id == default)
			{
				debugTexture = Raylib.LoadRenderTexture(windowWidth, windowHeight);
			}

			Raylib.BeginTextureMode(debugTexture);
			Raylib.ClearBackground(new Color(0, 0, 0, 40));

			Raylib.DrawTextEx(font, $"Torso position: {torsoCache.Position.X:F5} {torsoCache.Position.Y:F5} {torsoCache.Position.Z:F5}", 
				new Vector2(5, 5 + 14 * 0), 14, 1.4f, Color.White);

			Raylib.DrawTextEx(font, $"Torso rotation: {torsoCache.Rotation.X:F5} {torsoCache.Rotation.Y:F5} {torsoCache.Rotation.Z:F5}",
				new Vector2(5, 5 + 14 * 1), 14, 1.4f, Color.White);

			Raylib.DrawTextEx(font, $"Torso velocity: {torsoCache.LinearVelocity.X:F5} {torsoCache.LinearVelocity.Y:F5} {torsoCache.LinearVelocity.Z:F5}",
				new Vector2(5, 5 + 14 * 2), 14, 1.4f, Color.White);

			Raylib.DrawTextEx(font, $"Torso angular velocity: {torsoCache.AngularVelocity.X:F5} {torsoCache.AngularVelocity.Y:F5} {torsoCache.AngularVelocity.Z:F5}",
				new Vector2(5, 5 + 14 * 3), 14, 1.4f, Color.White);

			Raylib.DrawTextEx(font, $"Humanoid state: {currentState}",
				new Vector2(5, 5 + 14 * 4), 14, 1.4f, Color.White);

			Raylib.EndTextureMode();

			Raylib.DrawTextureRec(debugTexture.Texture, new Raylib_cs.Rectangle(0, windowHeight, windowWidth, -windowHeight), debugbillboardpos, Color.White);
		}
		public void Die()
		{
			if (GameManager.NetworkManager.IsServer)
			{
				var character = Parent as Model;
				var player = Root.GetService<Players>().GetPlayerFromCharacter(Parent);

				character?.BreakJoints();

				Task.Delay(4000).ContinueWith(_ =>
				{
					TaskScheduler.Schedule(() =>
					{
						Parent.Destroy();
						player.LoadCharacter();
					});
				});
			}
		}
	}
}
