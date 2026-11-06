using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

namespace NetBlox.Rendering;

public struct RealPlane
{
    public Vector3 normal;
    public float distance;
}

public struct Frustum
{
    public RealPlane plane_near;
    public RealPlane plane_far;
    public RealPlane plane_left;
    public RealPlane plane_right;
    public RealPlane plane_top;
    public RealPlane plane_bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RealPlane CreatePlane(Vector3 normal, Vector3 point)
    {
        normal = Vector3.Normalize(normal);
        return new RealPlane
        {
            normal = normal,
            distance = -Vector3.Dot(normal, point)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Frustum ExtractFrustum(float nearDistance, float farDistance, Camera3D camera)
    {
        Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        float verticalHalfFov = camera.FovY * (MathF.PI / 360.0f);
        float horizontalHalfFov = MathF.Atan(MathF.Tan(verticalHalfFov) * (Raylib.GetScreenWidth() / (float)Raylib.GetScreenHeight()));

        Vector3 leftRay = forward * MathF.Cos(horizontalHalfFov) - right * MathF.Sin(horizontalHalfFov);
        Vector3 rightRay = forward * MathF.Cos(horizontalHalfFov) + right * MathF.Sin(horizontalHalfFov);
        Vector3 topRay = forward * MathF.Cos(verticalHalfFov) + up * MathF.Sin(verticalHalfFov);
        Vector3 bottomRay = forward * MathF.Cos(verticalHalfFov) - up * MathF.Sin(verticalHalfFov);

        Vector3 nearCenter = camera.Position + forward * nearDistance;
        Vector3 farCenter = camera.Position + forward * farDistance;

        return new Frustum
        {
            plane_near = CreatePlane(forward, nearCenter),
            plane_far = CreatePlane(-forward, farCenter),
            plane_left = CreatePlane(Vector3.Cross(leftRay, up), camera.Position),
            plane_right = CreatePlane(Vector3.Cross(up, rightRay), camera.Position),
            plane_top = CreatePlane(Vector3.Cross(topRay, right), camera.Position),
            plane_bottom = CreatePlane(Vector3.Cross(right, bottomRay), camera.Position)
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool IsInFrustum(Vector3 min, Vector3 max)
    {
        Vector3 positiveVertex = default;

        positiveVertex.X = (plane_near.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_near.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_near.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_near.normal, positiveVertex) + plane_near.distance < 0)
            return false;

        positiveVertex.X = (plane_far.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_far.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_far.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_far.normal, positiveVertex) + plane_far.distance < 0)
            return false;

        positiveVertex.X = (plane_left.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_left.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_left.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_left.normal, positiveVertex) + plane_left.distance < 0)
            return false;

        positiveVertex.X = (plane_right.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_right.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_right.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_right.normal, positiveVertex) + plane_right.distance < 0)
            return false;

        positiveVertex.X = (plane_top.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_top.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_top.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_top.normal, positiveVertex) + plane_top.distance < 0)
            return false;

        positiveVertex.X = (plane_bottom.normal.X >= 0) ? max.X : min.X;
        positiveVertex.Y = (plane_bottom.normal.Y >= 0) ? max.Y : min.Y;
        positiveVertex.Z = (plane_bottom.normal.Z >= 0) ? max.Z : min.Z;

        if (Vector3.Dot(plane_bottom.normal, positiveVertex) + plane_bottom.distance < 0)
            return false;

        return true;
    }
}
