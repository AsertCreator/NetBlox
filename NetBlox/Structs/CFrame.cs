using Raylib_cs;
using System.Numerics;

namespace NetBlox.Structs
{
	public struct CFrame
	{
		public Vector3 Position;
		public Quaternion Rotation;

		public CFrame(Vector3 pos)
		{
			Position = pos;
			Rotation = Quaternion.Identity;
		}
		public CFrame(Vector3 pos, Quaternion rot)
		{
			Position = pos;
			Rotation = rot;
		}

		public static CFrame operator *(CFrame a, CFrame b) => new () 
		{ 
			Position = a.Position + b.Position,
			Rotation = a.Rotation * b.Rotation
		};
		public static CFrame operator -(CFrame a, CFrame b) => new()
		{
			Position = a.Position - b.Position,
			Rotation = a.Rotation / b.Rotation
		};
	}
}
