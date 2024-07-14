using System;
using System.Collections.Generic;
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
		public static Quaternion FromMatrix(float[] x, float[] y, float[] z)
		{
			float b1_squared = 0.25f * (1.0f + x[0] + y[1] + z[2]);
			if (b1_squared >= 0.25f)
			{
				// Equation (164)
				float b1 = MathF.Sqrt(b1_squared);

				float over_b1_4 = 0.25f / b1;
				float b2 = (z[1] - y[2]) * over_b1_4;
				float b3 = (x[2] - z[0]) * over_b1_4;
				float b4 = (y[1] - z[2]) * over_b1_4;

				// Return the quaternion
				return new Quaternion(b1, b2, b3, b4);
			}
			return Quaternion.Identity;
		}
	}
}
