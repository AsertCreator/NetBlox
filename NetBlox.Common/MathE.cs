using System.Numerics;
using System.Text;

namespace NetBlox.Common
{
	public static class MathE
	{
		public static float Lerp(float a, float b, float t) => a * (1 - t) + b * t;
		public static string Roll(string str, byte a)
		{
			var bytes = Encoding.ASCII.GetBytes(str);
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] ^= a;
			return Encoding.ASCII.GetString(bytes);
		}
		public static string FormatSize(long bytes)
		{
			if (bytes < 1024) return bytes + " bytes";
			if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F2") + " kbs";
			if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("F2") + " mbs";
			return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " gbs";
		}
		public static Quaternion QuaternionFromMatrix(float[] x, float[] y, float[] z)
		{
			Matrix4x4 mat;
			mat.M11 = x[0]; mat.M12 = x[1]; mat.M13 = x[2]; mat.M14 = 0;
			mat.M21 = y[0]; mat.M22 = y[1]; mat.M23 = y[2]; mat.M24 = 0;
			mat.M31 = z[0]; mat.M32 = z[1]; mat.M33 = z[2]; mat.M34 = 0;
			mat.M41 = 0; mat.M42 = 0; mat.M43 = 0; mat.M44 = 1;
			return Quaternion.CreateFromRotationMatrix(mat);
		}
		public static Vector3 ToDegrees(Vector3 rotrad) => new Vector3()
		{
			X = rotrad.X * 57.2958f,
			Y = rotrad.Y * 57.2958f,
			Z = rotrad.Z * 57.2958f
		};
	}
}
