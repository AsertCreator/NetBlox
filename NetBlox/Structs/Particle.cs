using NetBlox.Instances;
using NetBlox.Instances.Effects;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Structs
{
	public class Particle : I3DRenderable
	{
		public GameManager GameManager;
		public IEffectEmitter Emitter;
		public Vector3 Position;
		public Vector3 LinearVelocity;
		public float Rotation;
		public float RotationalVelocity;
		public float LifetimeinSeconds;
		public float DeathLifetimeInSeconds;
		public float Opacity;
		public Color Color;
		public Texture2D Texture;

		public Particle(IEffectEmitter emitter)
		{
			Emitter = emitter;
			Position = emitter.GetStartPosition();
			LinearVelocity = emitter.GetStartLinearVelocity();
			Rotation = emitter.GetStartRotation();
			RotationalVelocity = emitter.GetStartRotationalVelocity();
			LifetimeinSeconds = 0;
			DeathLifetimeInSeconds = emitter.GetLifetimeinSeconds();
			Opacity = emitter.GetStartOpacity();
			Color = emitter.GetStartColor();
			Texture = emitter.GetTexture();
		}

		public void Step(float deltaTime)
		{
			Position += LinearVelocity * deltaTime;
			LinearVelocity += Emitter.GetAcceleration() * deltaTime;
			Rotation += RotationalVelocity * deltaTime;
			Opacity += Emitter.GetOpacityVelocity() * deltaTime;
			LifetimeinSeconds += deltaTime;

			if (LifetimeinSeconds >= DeathLifetimeInSeconds)
			{
				GameManager.RenderManager.RemoveParticle(this);
				return;
			}
		}
		public void Render()
		{
			var camera = GameManager.RenderManager.MainCamera;
			var direction = camera.Target - camera.Position;
			RenderUtils.DrawCubeTextureRec(Texture, Position,
				Raymath.QuaternionFromVector3ToVector3(Vector3.UnitX, direction), 1, 1, 1, Color.Brown, Faces.All, false);
		}
	}
}
