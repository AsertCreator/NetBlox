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
}