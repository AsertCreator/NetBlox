using Raylib_cs;
using System.Numerics;

namespace NetBlox.Structs
{
	public struct UDim2
	{
		public float X;
		public float Y;
		public float XOff;
		public float YOff;
		public UDim2(float x, float y) { X = x; Y = y; XOff = 0; YOff = 0; }
		public UDim2(float x, float x2, float y, float y2) { X = x; Y = y; XOff = x2; YOff = y2; }
		public Vector2 Calculate()
		{
			return new Vector2()
			{
				X = X * Raylib.GetScreenWidth() + XOff,
                Y = Y * Raylib.GetScreenHeight() + YOff
            };
		}
	}
}
