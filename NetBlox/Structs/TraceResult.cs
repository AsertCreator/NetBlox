using System.Diagnostics;
using System.Numerics;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;

namespace NetBlox.Structs;

public delegate bool TraceCallback(BasePart? basePart, ref TraceResult result);

public struct TraceResult
{
    public Workspace Workspace;
    public Vector3 Start;
    public Vector3 End;
    public Vector3 Normal;
    public TraceCallback? Callback;
    public float Fraction;
    public BasePart? HitPart;
}