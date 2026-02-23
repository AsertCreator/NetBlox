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
	public class ForceField : Instance, IEffectEmitter, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public bool Enabled { get; set; } = true;

		private Stopwatch particleStopwatch;
		private static Texture2D ForceFieldTexture;
		private static Random random = new();

		static ForceField()
		{
			RenderManager.LoadTexture("rbxasset://textures/particleForceField.png", x => ForceFieldTexture = x);
		}
		public ForceField(GameManager ins) : base(ins)
		{
			GameManager.RenderManager.Visibles3DGrade0.Add(this);
			particleStopwatch = new();
			particleStopwatch.Start();
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Smoke) == classname) return true;
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
				if (particleStopwatch.ElapsedMilliseconds > 1000 / MathE.Clamp(10, 50, AppManager.PreferredFPS))
				{
					particleStopwatch.Reset();
					particleStopwatch.Start();

					for (int i = 0; i < 5; i++)
					{
						var particle = new Particle(this);
						GameManager.RenderManager.AddParticle(particle);
					}
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
		public Vector3 GetStartLinearVelocity() 
		{
			var vector = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
			vector.X -= 0.5f;
			vector.Y -= 0.5f;
			vector.Z -= 0.5f;
			if (vector.X == 0 && vector.Y == 0 && vector.Z == 0)
				vector.X = 1;
			return Vector3.Normalize(vector) * 5;
		}
		public Vector3 GetAcceleration() => new Vector3(0, 0, 0);
		public float GetLifetimeinSeconds() => 0.6f;
		public float GetStartOpacity() => 0.8f;
		public float GetOpacityVelocity() => -0.8f / GetLifetimeinSeconds();
		public Color GetStartColor() => Color.White;
		public float GetStartRotation() => random.NextSingle() * 360;
		public float GetStartRotationalVelocity() => 0;
		public Texture2D GetTexture() => ForceFieldTexture;
		public float GetSize() => 0.2f;
	}
}
