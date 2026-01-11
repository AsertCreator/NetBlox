using NetBlox.Common;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

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
		public float Health 
		{
			get => health;
			set
			{
				health = value;
				if (value > 0)
					EverHadHealthHigherThan0 = true;
			}
		}
		[Lua([Security.Capability.None])]
		public float WalkSpeed { get; set; } = 12;
		[Lua([Security.Capability.None])]
		public float JumpPower { get; set; } = 6;
		[Lua([Security.Capability.None])]
		public Vector3 WalkToPoint 
		{ 
			get 
			{
				return walkToPointValue;
			} 
			set 
			{
				// asdsafsasffsfsdfdsdfsgsdgadjghfsdjfdfadshfiarhovvioeevnefven im so done with this
				// todo: do some expected networking bullshit here instead of this

				walkToPointValue = value;
			} 
		}

		private Vector3 walkToPointValue;

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

		public Job HumanoidMovementJob;

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

		private float health = 100;
		private bool EverHadHealthHigherThan0;
		private HumanoidState currentState = HumanoidState.Idle;
		private RenderTexture2D debugHudTexture;
		private Vector3 privateMoveToTarget;

		public Humanoid(GameManager ins) : base(ins)
		{
			LeftArm = new InstanceSiblingHandle<BasePart>(this, "Left Arm");
			RightArm = new InstanceSiblingHandle<BasePart>(this, "Right Arm");
			LeftLeg = new InstanceSiblingHandle<BasePart>(this, "Left Leg");
			RightLeg = new InstanceSiblingHandle<BasePart>(this, "Right Leg");
			Torso = new InstanceSiblingHandle<BasePart>(this, "Torso");
			Head = new InstanceSiblingHandle<BasePart>(this, "Head");

			void OnLimbAttached(object? _, Instance limbPart)
			{
				BasePart typedLimbPart = (BasePart)limbPart;

				LogManager.LogInfo("AAAAAAAAA-0000000");

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

				LogManager.LogInfo("AAAAAAAAA-0000001");

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

				LogManager.LogInfo("AAAAAAAAA-0000002");

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

			LeftArm.OnSiblingReleased += OnLimbDeattachedGeneric;
			RightArm.OnSiblingReleased += OnLimbDeattachedGeneric;
			LeftLeg.OnSiblingReleased += OnLimbDeattachedGeneric;
			RightLeg.OnSiblingReleased += OnLimbDeattachedGeneric;
			Torso.OnSiblingReleased += OnLimbDeattachedLifeCritical;
			Head.OnSiblingReleased += OnLimbDeattachedLifeCritical;

			LeftArm.Reactivate();
			RightArm.Reactivate();
			LeftLeg.Reactivate();
			RightLeg.Reactivate();
			Torso.Reactivate();
			Head.Reactivate();

			HumanoidMovementJob = TaskScheduler.ScheduleNamedJob("HumanoidMovement", JobType.Physics, _ =>
			{
				if (WasDestroyed)
					return JobResult.CompletedSuccess;

				if (!Torso.IsPresent)
					return JobResult.NotCompleted;

				var delta = (Torso.WantedSibling.Position - WalkToPoint) * AppManager.DeltaFactor();

				Torso.WantedSibling.Position += delta;

				return JobResult.NotCompleted;
			});
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

		// ideally localplayer humanoid should be controlled by the corescripts
		// but im lazy and i hate lua

		public override void Process()
		{
			base.Process();

			if (Parent == null)
				return;

			if (IsLocalPlayer && Torso.IsPresent)
			{
				ProcessInput();
			}
		}
		private void ProcessInput()
		{
			var forwardsPressed = ControlForward.IsPressed();
			var strafeLeftPressed = ControlLeft.IsPressed();
			var strafeRightPressed = ControlRight.IsPressed();
			var backwardsPressed = ControlBackward.IsPressed();

			Vector2 generalDirection = default;

			if (forwardsPressed)
				generalDirection += new Vector2(0, 1);
			if (strafeLeftPressed)
				generalDirection += new Vector2(-1, 0);
			if (strafeRightPressed)
				generalDirection += new Vector2(1, 0);
			if (backwardsPressed)
				generalDirection += new Vector2(0, -1);

			if (generalDirection != default)
				generalDirection = Vector2.Normalize(generalDirection);

			Vector3 translated = GetWalkingPosition();
			translated += new Vector3(generalDirection.Y, 0, generalDirection.X);

			MoveTo(translated);
		}
		private Vector3 GetWalkingPosition()
		{
			return LeftLeg.WantedSibling.Position - new Vector3(0, RightLeg.WantedSibling.Size.Y / 2, 0);
		}

		[Lua([Security.Capability.None])]
		public void MoveTo(Vector3 to)
		{
			privateMoveToTarget = to;
		}
		[Lua([Security.Capability.None])]
		public void TakeDamage(float am)
		{
			Health -= am;
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
			else
			{
				Raylib.DrawTextEx(font, $"Torso not found: {torso}", new Vector2(5, 5 + 14 * 0), 14, 1.4f, Color.Red);
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

				if (EverHadHealthHigherThan0 && Torso.IsPresent && Torso.WantedSibling.IsDomestic)
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
	}
}
