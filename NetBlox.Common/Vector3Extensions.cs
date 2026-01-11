using NetBlox.Common;
using System.Numerics;

namespace NetBlox
{
	public static class Vector3Extensions
	{
		public static bool IsPhysicallyClose(this Vector3 a, Vector3 b)
		{
			return a.X.IsAround(b.X, 0.005f) && a.Y.IsAround(b.Y, 0.005f) && a.Z.IsAround(b.Z, 0.005f);
		}
	}
}
