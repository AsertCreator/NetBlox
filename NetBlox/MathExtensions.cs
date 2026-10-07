using System.Numerics;

namespace NetBlox;

public static class MathExtensions
{
    public static bool WithinError(this float a, float b, float error)
    {
        a = MathF.Abs(a);
        b = MathF.Abs(b);
        if (a + error <= b - error)
            return true;
        if (a - error <= b + error)
            return true;
        return false;
    }
    public static Vector3 Snap(this Vector3 v3, float snap)
    {
        return new Vector3(
            (int)(v3.X / snap) * snap,
            (int)(v3.Y / snap) * snap,
            (int)(v3.Z / snap) * snap
        );
    }
}