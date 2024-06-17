using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Common
{
	public static class MathE
	{
		public static float Lerp(float a, float b, float t) => a * (1 - t) + b * t;
	}
}
