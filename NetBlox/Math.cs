using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox
{
	public static class Math
	{
		public static float Lerp(float a, float b, float t)
		{
			return a * (1 - t) + b * t;
		}
	}
}
