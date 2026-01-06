using NetBlox.Common;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using Raylib_cs;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
				if (value is not Model model)
				{
					LogManager.LogWarn("Humanoid instance must be attached to a Model!");
					return;
				}

				// this will break something very niche. i swear i will not fix it.

				if (model.FindFirstAncestorOfClass("Humanoid") != null)
				{
					LogManager.LogWarn("Cannot attach a second Humanoid instance to a Model!");
					return;
				}

				base.Parent = value;
			}
		}

		public InstanceSiblingHandle<BasePart> LeftArm;
		public InstanceSiblingHandle<BasePart> RightArm;
		public InstanceSiblingHandle<BasePart> LeftLeg;
		public InstanceSiblingHandle<BasePart> RightLeg;
		public InstanceSiblingHandle<BasePart> Torso;
		public InstanceSiblingHandle<BasePart> Head;

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

				ReplicateProperties(["State"], true);
			}
		}

		private HumanoidState currentState = HumanoidState.Idle;
		private bool isDying = false;
		private RenderTexture2D debugHudTexture;
		private Vector3 humanoidCenterPosition;

		public Humanoid(GameManager ins) : base(ins)
		{
			InitiliazeHumanoid();
		}
		public void InitiliazeHumanoid()
		{
			LeftArm = new InstanceSiblingHandle<BasePart>(this, "Left Arm");
			RightArm = new InstanceSiblingHandle<BasePart>(this, "Right Leg");
			LeftLeg = new InstanceSiblingHandle<BasePart>(this, "Left Arm");
			RightLeg = new InstanceSiblingHandle<BasePart>(this, "Right Leg");
			Torso = new InstanceSiblingHandle<BasePart>(this, "Torso");
			Head = new InstanceSiblingHandle<BasePart>(this, "Head");

			void OnLimbAttached(object? _, Instance limbPart)
			{
				BasePart typedLimbPart = (BasePart)limbPart;

				if (typedLimbPart.IsHumanoidLimb)
				{
					LogManager.LogWarn($"Limb of Humanoid \"{GetFullName()}\", \"{typedLimbPart.Name}\" is already attached to some Humanoid; what is going on?");
					return;
				}

				typedLimbPart.IsHumanoidLimb = true;
			}
			void OnLimbDeattachedGeneric(object? _, Instance limbPart)
			{
				BasePart typedLimbPart = (BasePart)limbPart;

				if (!typedLimbPart.IsHumanoidLimb)
				{
					LogManager.LogWarn($"Limb of Humanoid \"{GetFullName()}\", \"{typedLimbPart.Name}\" is already deattached; what is going on?");
					return;
				}

				typedLimbPart.IsHumanoidLimb = false;
			}
			void OnLimbDeattachedLifeCritical(object? _, Instance limbPart)
			{
				BasePart typedLimbPart = (BasePart)limbPart;

				if (!typedLimbPart.IsHumanoidLimb)
				{
					LogManager.LogWarn($"Limb of Humanoid \"{GetFullName()}\", \"{typedLimbPart.Name}\" is already deattached; what is going on? Life critical btw");
					return;
				}

				typedLimbPart.IsHumanoidLimb = false;

				// actually let's die

				ResetCharacter();
			}

			LeftArm.OnSiblingTaken += OnLimbAttached;
			RightArm.OnSiblingTaken += OnLimbAttached;
			LeftLeg.OnSiblingTaken += OnLimbAttached;
			RightLeg.OnSiblingTaken += OnLimbAttached;
			Torso.OnSiblingTaken += OnLimbAttached;
			Head.OnSiblingTaken += OnLimbAttached;

			LeftArm.OnSiblingTaken += OnLimbDeattachedGeneric;
			RightArm.OnSiblingTaken += OnLimbDeattachedGeneric;
			LeftLeg.OnSiblingTaken += OnLimbDeattachedGeneric;
			RightLeg.OnSiblingTaken += OnLimbDeattachedGeneric;
			Torso.OnSiblingTaken += OnLimbDeattachedLifeCritical;
			Head.OnSiblingTaken += OnLimbDeattachedLifeCritical;
		}

		[Lua([Security.Capability.CoreSecurity])]
		public void ResetCharacter()
		{
			if (GameManager.NetworkManager.IsClient)
				GameManager.NetworkManager.SendServerboundPacket(NPCharacterReset.Create(Parent));
			if (GameManager.NetworkManager.IsServer)
			{
				Health = 0;
				ReplicateProperties(["Health"], true);
			}
		}

		public override void Process()
		{
			base.Process();

			if (Parent == null)
			{

			}
		}
		private void ProcessInput()
		{

		}
		public void DoStateTransition(HumanoidState old, HumanoidState newv)
		{

		}
		public override void Destroy()
		{
			LeftArm.Dispose();
			RightArm.Dispose();
			LeftLeg.Dispose();
			RightLeg.Dispose();
			Torso.Dispose();
			Head.Dispose();
			
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
			var pos3d = head.Position + new Vector3(0, head.Size.Y / 2 + 1f, 0);
			var pos = Raylib.GetWorldToScreen(pos3d, cam);
			var siz = Vector2.Zero;

			if (I3DRenderable.IsLookingTowards(pos3d, GameManager))
			{
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
			var debugbillboard3dpos = head.Position + new Vector3(0, head.Size.Y / 2 + 2f, 0);
			var debugbillboardpos = Raylib.GetWorldToScreen(debugbillboard3dpos, cam);
			var scale = 10 / Vector3.Distance(cam.Position, debugbillboard3dpos);
			var font = GameManager.RenderManager.MainFont14.SpriteFont;

			debugbillboardpos += new Vector2(-windowWidth / 2, -windowHeight / 1.2f);

			if (debugHudTexture.Id == default)
			{
				debugHudTexture = Raylib.LoadRenderTexture(windowWidth, windowHeight);
			}

			if (!I3DRenderable.IsLookingTowards(debugbillboard3dpos, GameManager))
				return;

			Raylib.BeginTextureMode(debugHudTexture);
			Raylib.ClearBackground(new Color(0, 0, 0, 40));

			var torso = Torso.WantedSibling;

			if (torso != null)
			{
				Raylib.DrawTextEx(font, $"Torso position: {torso.Position.X:F5} {torso.Position.Y:F5} {torso.Position.Z:F5}",
					new Vector2(5, 5 + 14 * 0), 14, 1.4f, Color.White);

				Raylib.DrawTextEx(font, $"Torso rotation: {torso.Rotation.X:F5} {torso.Rotation.Y:F5} {torso.Rotation.Z:F5}",
					new Vector2(5, 5 + 14 * 1), 14, 1.4f, Color.White);

				Raylib.DrawTextEx(font, $"Torso velocity: {torso.LinearVelocity.X:F5} {torso.LinearVelocity.Y:F5} {torso.LinearVelocity.Z:F5}",
					new Vector2(5, 5 + 14 * 2), 14, 1.4f, Color.White);

				Raylib.DrawTextEx(font, $"Torso angular velocity: {torso.AngularVelocity.X:F5} {torso.AngularVelocity.Y:F5} {torso.AngularVelocity.Z:F5}",
					new Vector2(5, 5 + 14 * 3), 14, 1.4f, Color.White);

				Raylib.DrawTextEx(font, $"Humanoid state: {currentState}",
					new Vector2(5, 5 + 14 * 4), 14, 1.4f, Color.White);
			}

			Raylib.EndTextureMode();

			Raylib.DrawTextureRec(debugHudTexture.Texture, new Raylib_cs.Rectangle(0, windowHeight, windowWidth, -windowHeight), debugbillboardpos, Color.White);
		}
		public void Die()
		{
			if (GameManager.NetworkManager.IsServer)
			{
				var character = Parent as Model;
				var player = Root.GetService<Players>().GetPlayerFromCharacter(Parent);

				if (LeftArm.IsPresent)
					LeftArm.WantedSibling.IsHumanoidLimb = false;
				if (RightArm.IsPresent)
					RightArm.WantedSibling.IsHumanoidLimb = false;
				if (LeftLeg.IsPresent)
					LeftLeg.WantedSibling.IsHumanoidLimb = false;
				if (RightLeg.IsPresent)
					RightLeg.WantedSibling.IsHumanoidLimb = false;
				if (Torso.IsPresent)
					Torso.WantedSibling.IsHumanoidLimb = false;
				if (Head.IsPresent)
					Head.WantedSibling.IsHumanoidLimb = false;

				character?.BreakJoints();

				if (player != null)
				{
					TaskScheduler.ScheduleDelayedNamedJob("HumanoidDeath", new TimeSpan(0, 0, 4), JobType.Miscellaneous, x =>
					{
						if (player.WasDestroyed)
							return JobResult.CompletedFailure;

						Parent.Destroy();
						player.LoadCharacter();

						GameManager.NetworkManager.AddReplication(player.Character!,
							Replication.REPM_TOALL,
							Replication.REPW_NEWINST);

						return JobResult.CompletedSuccess;
					});
				}
			}
		}

		//
		// do not call things in here
		//

		private void DoWalkingOld()
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
				StandUpOld();

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
				Torso.WantedSibling.Velocity = new Vector3(MathE.Lerp(Torso.WantedSibling.LinearVelocity.X, veldelta.X, 0.7f),
				 	Torso.WantedSibling.LinearVelocity.Y, MathE.Lerp(Torso.WantedSibling.LinearVelocity.Z, veldelta.Z, 0.7f));

				if (Torso.WantedSibling.BodyHandle.HasValue)
				{
					Vector3 characterForward = Raymath.Vector3RotateByQuaternion(Vector3.UnitZ, Torso.WantedSibling.QuaternionRotation);
					Vector3 cameraForward = GameManager.RenderManager.MainCamera.Target - GameManager.RenderManager.MainCamera.Position;
					cameraForward.Y = 0;
					cameraForward = Vector3.Normalize(cameraForward);
					characterForward.Y = 0;
					characterForward = Vector3.Normalize(characterForward);

					float anglediff = MathF.Atan2(
						cameraForward.X * characterForward.Z - cameraForward.Z * characterForward.X,
						cameraForward.X * characterForward.X + cameraForward.Z * characterForward.Z);
					float angular_velocity = MathE.Clamp(-5, anglediff, 5);

					var angular = Torso.WantedSibling.AngularVelocity;
					angular.Y = angular_velocity * 3;
					var body = GameManager.PhysicsManager.LocalSimulation.Bodies[Torso.WantedSibling.BodyHandle.Value];
					body.ApplyAngularImpulse(angular);
					body.Awake = true;
				}
			}
			else
			{
				Torso.WantedSibling.Velocity = new Vector3(Torso.WantedSibling.LinearVelocity.X / 8,
					Torso.WantedSibling.LinearVelocity.Y, Torso.WantedSibling.LinearVelocity.Z / 8);
			}
		}
		private void StabilizeHumanoidOld()
		{
			if (Torso.WantedSibling.BodyHandle.HasValue)
			{
				// may chatgpt help me
				Vector3 up = Raymath.Vector3RotateByQuaternion(Vector3.UnitY, Torso.WantedSibling.QuaternionRotation);
				Vector3 correctionAxis = Raymath.Vector3CrossProduct(up, Vector3.UnitY);
				float angleError = MathF.Acos(Raymath.Vector3DotProduct(up, Vector3.UnitY));
				var torque = Raymath.Vector3Normalize(correctionAxis) * (angleError * 240) - Torso.WantedSibling.AngularVelocity * 1f;
				if (float.IsNaN(torque.X) || float.IsNaN(torque.Y) || float.IsNaN(torque.Z))
					return;
				if (float.IsInfinity(torque.X) || float.IsInfinity(torque.Y) || float.IsInfinity(torque.Z))
					return;
				Torso.WantedSibling.AngularVelocity += torque;
				Torso.WantedSibling.Velocity += new Vector3(0, 0.1f, 0);
			}
		}
		private void StandUpOld()
		{
			var part = Torso.WantedSibling;
			part.Velocity += new Vector3(0, 35, 0);
			if (part.Velocity.Y >= 38)
				part.Velocity = new Vector3(part.Velocity.X, 38, part.Velocity.Z);

			State = HumanoidState.Jumping;
		}
	}
}
