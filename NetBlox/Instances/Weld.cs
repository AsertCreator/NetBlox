using BepuPhysics;
using BepuPhysics.Constraints;
using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Weld : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? Part0 { get; set; }
		[Lua([Security.Capability.None])]
		public Instance? Part1 { get; set; }
		[Lua([Security.Capability.None])]
		public bool Enabled 
		{
			get => enabled;
			set
			{
				if (value == enabled)
					return;

				if (IsDomestic)
				{
					if (value)
						CreateWeld();
					else
						DestroyWeld();
				}

				enabled = value;
			}
		}
		private BallSocket ballSocket;
		private AngularMotor angularMotor;
		private ConstraintHandle ballSocketHandle;
		private ConstraintHandle angularMotorHandle;
		private bool enabled;

		public Weld(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Weld) == classname) return true;
			return base.IsA(classname);
		}
		public override void OnNetworkOwnershipChanged()
		{
			TaskScheduler.Schedule(() =>
			{
				if (IsDomestic)
				{
					if (Enabled)
						CreateWeld();
					else
						DestroyWeld();
				}
			});
		}
		private void DestroyWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;

			sim.Solver.Remove(ballSocketHandle);
			sim.Solver.Remove(angularMotorHandle);
		}
		private void CreateWeld()
		{
			var sim = GameManager.PhysicsManager.LocalSimulation;
			var b0 = Part0 as BasePart;
			var b1 = Part1 as BasePart;

			if (Part0 == Part1)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld cannot be set to the same part!");
				return;
			}

			if (b0 == null || b1 == null)
			{
				LogManager.LogWarn("Part0 and Part1 properties of Weld only support BaseParts!");
				return;
			}

			ballSocket = new BallSocket()
			{
				LocalOffsetA = default,
				LocalOffsetB = default,
				SpringSettings = new SpringSettings(30, 1)
			};
			angularMotor = new AngularMotor()
			{
				Settings = new MotorSettings() { Damping = 1, Softness = 0 },
				TargetVelocityLocalA = default
			};

			ballSocketHandle = sim.Solver.Add(b0.BodyHandle.Value, b1.BodyHandle.Value, ballSocket);
			angularMotorHandle = sim.Solver.Add(b0.BodyHandle.Value, b1.BodyHandle.Value, angularMotor);
		}
	}
}
