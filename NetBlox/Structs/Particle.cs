using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Effects;
using Raylib_cs;
using System.Numerics;

using Rectangle = Raylib_cs.Rectangle;

namespace NetBlox.Structs
{
	public class Particle
	{
		public IEffectEmitter Emitter;
		public Vector3 Position;
		public Vector3 LinearVelocity;
		/// <summary>
		/// Rotation in degrees
		/// </summary>
		public float Rotation;
		/// <summary>
		/// Rotational velocity in degrees/second
		/// </summary>
		public float RotationalVelocity;
		public float LifetimeinSeconds;
		public float DeathLifetimeInSeconds;
		public float Opacity;
		public float Size;
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
			Size = emitter.GetSize();
		}

		public void Step(float deltaTime)
		{
			Position += LinearVelocity * deltaTime;
			LinearVelocity += Emitter.GetAcceleration() * deltaTime;
			Rotation += RotationalVelocity * deltaTime;
			Opacity += Emitter.GetOpacityVelocity() * deltaTime;
			LifetimeinSeconds += deltaTime;
		}
		public void Render(Camera3D camera)
		{
			Opacity = MathE.Clamp(0, Opacity, 1);
			Raylib.DrawBillboardPro(
				camera, Texture, new Rectangle(0, 0, Texture.Width, Texture.Height), Position, camera.Up, 
				new Vector2(Size, Size), new Vector2(Texture.Width, Texture.Height) / 2, 0,
				new Color(Color.R, Color.G, Color.B, (int)(Opacity * 255))
			);
		}
	}
}
