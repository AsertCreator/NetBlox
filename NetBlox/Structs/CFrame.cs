using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Structs
{
	public struct CFrame(Vector3 pos, Quaternion rot = default)
	{
		public Vector3 Position = pos;
		public Quaternion Rotation = rot;

		public static CFrame operator *(CFrame a, CFrame b) => new () 
		{ 
			Position = a.Position + b.Position,
			Rotation = a.Rotation + b.Rotation
		};
		public static CFrame operator -(CFrame a, CFrame b) => new()
		{
			Position = a.Position - b.Position,
			Rotation = a.Rotation - b.Rotation
		};
	}
}
