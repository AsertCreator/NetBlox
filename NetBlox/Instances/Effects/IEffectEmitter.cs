using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

namespace NetBlox.Instances.Effects
{
	// hmmmm
	public interface IEffectEmitter
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetStartPosition();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetStartLinearVelocity();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3 GetAcceleration();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetLifetimeinSeconds();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetStartOpacity();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetOpacityVelocity();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color GetStartColor();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetStartRotation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetStartRotationalVelocity();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetSize();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Texture2D GetTexture();
	}
}
