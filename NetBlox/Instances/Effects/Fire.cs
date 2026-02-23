using NetBlox.Common;
using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Instances.Parts;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace NetBlox.Instances.Effects
{
	[Creatable]
	public class Fire : Instance, IEffectEmitter, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public Color Color { get; set; } = Color.Orange;
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;
		[Lua([Security.Capability.None])]
		public float RiseVelocity { get; set; } = 5;
		[Lua([Security.Capability.None])]
		public float Size { get; set; } = 1.2f;
		[Lua([Security.Capability.None])]
		public float TimeScale { get; set; } = 1;

		private Stopwatch particleStopwatch;
		private static Texture2D FireTexture;
		private static Random random = new();

		static Fire()
		{
			RenderManager.LoadTexture("rbxasset://textures/particleFire.png", x => FireTexture = x);
		}
		public Fire(GameManager ins) : base(ins)
		{
			GameManager.RenderManager.Visibles3DGrade0.Add(this);
			particleStopwatch = new();
			particleStopwatch.Start();
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Fire) == classname) return true;
			return base.IsA(classname);
		}
		public override void Destroy()
		{
			GameManager.RenderManager.Visibles3DGrade0.Remove(this);
		}

		public void Render()
		{
			if (!Enabled)
				return;

			if (IsDescendantOfWorkspace())
			{
				if (particleStopwatch.ElapsedMilliseconds > 1000 / MathE.Clamp(10, 60, AppManager.PreferredFPS))
				{
					particleStopwatch.Reset();
					particleStopwatch.Start();

					var particle = new Particle(this);
					GameManager.RenderManager.AddParticle(particle);
				}
			}
		}
		public Vector3? GetRootPosition()
		{
			if (Parent is BasePart bp)
				return bp.Position;
			return null;
		}
		public Vector3 GetStartPosition()
		{
			// we are not supposed to be called if we are not enabled (either Enabled == false or Parent is not BasePart)
			return GetRootPosition().Value +
				new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f);
		}
		public Vector3 GetStartLinearVelocity() => new Vector3(random.NextSingle() - 0.5f, RiseVelocity * TimeScale, random.NextSingle() - 0.5f);
		public Vector3 GetAcceleration() => new Vector3(0, 0, 0);
		public float GetLifetimeinSeconds() => 1 / TimeScale;
		public float GetStartOpacity() => 0.5f;
		public float GetOpacityVelocity() => -0.5f / GetLifetimeinSeconds();
		public Color GetStartColor() => Color;
		public float GetStartRotation() => random.NextSingle() * 360;
		public float GetStartRotationalVelocity() => 0;
		public Texture2D GetTexture() => FireTexture;
		public float GetSize() => Size;
	}
}
