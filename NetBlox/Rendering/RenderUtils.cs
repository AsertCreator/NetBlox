using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox.Rendering
{
    // straight from gen 1
    public unsafe static class RenderUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void DrawCubeTextureRec(Texture2D texture, Vector3 position, Quaternion rotation, float width, float height, float length, Color color, Faces f, bool tile = false, bool interpolate = false)
        {
            Vector3 axis;
            float angle;
            Raymath.QuaternionToAxisAngle(rotation, &axis, &angle);

            Rlgl.PushMatrix();
            Rlgl.MatrixMode(MatrixMode.Texture);
            Rlgl.Translatef(position.X, position.Y, position.Z);
            Rlgl.Rotatef(angle * 180 / MathF.PI, axis.X, axis.Y, axis.Z);
            // im not willing to rewrite the whole shit
            position = Vector3.Zero;

            if (f != 0)
            {
                float x = position.X;
                float y = position.Y;
                float z = position.Z;

                Rlgl.SetTexture(texture.Id);

                Rlgl.Begin(7);
                Rlgl.Color4ub(color.R, color.G, color.B, color.A);

                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_REPEAT);
                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_REPEAT);
                if (interpolate)
                {
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MIN_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MAG_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                }

                // NOTE: Enable texture 1 for Front, Back
                Rlgl.EnableTexture(texture.Id);

                if ((f & Faces.Front) != 0)
                {
                    // Front Face
                    // Normal Pointing Towards Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
                }

                if ((f & Faces.Back) != 0)
                {
                    // Back Face
                    // Normal Pointing Away From Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
                }

                if ((f & Faces.Top) != 0)
                {
                    // Top Face
                    // Normal Pointing Up
                    Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
                }

                if ((f & Faces.Bottom) != 0)
                {
                    // Bottom Face
                    // Normal Pointing Down
                    Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
                }

                if ((f & Faces.Right) != 0)
                {
                    // Right face
                    // Normal Pointing Right
                    Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
                }

                if ((f & Faces.Left) != 0)
                {
                    // Left Face
                    // Normal Pointing Left
                    Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
                    Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
                }

                Rlgl.End();

                Rlgl.DisableTexture();
            }

            Rlgl.PopMatrix();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void DrawCubeTextureRec(Texture2D texture, float width, float height, float length, Color color, Faces f, bool tile = false, bool interpolate = false)
        {
            if (f != 0)
            {
                Rlgl.SetTexture(texture.Id);

                Rlgl.Begin(7);
                Rlgl.Color4ub(color.R, color.G, color.B, color.A);

                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_REPEAT);
                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_REPEAT);
                if (interpolate)
                {
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MIN_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MAG_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                }

                // NOTE: Enable texture 1 for Front, Back
                Rlgl.EnableTexture(texture.Id);

                if ((f & Faces.Front) != 0)
                {
                    // Front Face
                    // Normal Pointing Towards Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);
                }

                if ((f & Faces.Back) != 0)
                {
                    // Back Face
                    // Normal Pointing Away From Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);
                }

                if ((f & Faces.Top) != 0)
                {
                    // Top Face
                    // Normal Pointing Up
                    Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);
                }

                if ((f & Faces.Bottom) != 0)
                {
                    // Bottom Face
                    // Normal Pointing Down
                    Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length : -1.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);
                }

                if ((f & Faces.Right) != 0)
                {
                    // Right face
                    // Normal Pointing Right
                    Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);
                }

                if ((f & Faces.Left) != 0)
                {
                    // Left Face
                    // Normal Pointing Left
                    Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);
                }

                Rlgl.End();

                Rlgl.DisableTexture();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void DrawCubeTextureRec2(Texture2D texture, float width, float height, float length, Color color, Faces f, bool tile = false, bool interpolate = false)
        {
            if (f != 0)
            {
                Rlgl.SetTexture(texture.Id);

                Rlgl.Begin(7);
                Rlgl.Color4ub(color.R, color.G, color.B, color.A);

                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_REPEAT);
                Rlgl.TextureParameters(0, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_REPEAT);
                if (interpolate)
                {
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MIN_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                    Rlgl.TextureParameters(0, Rlgl.TEXTURE_MAG_FILTER, Rlgl.TEXTURE_FILTER_LINEAR);
                }

                // NOTE: Enable texture 1 for Front, Back
                Rlgl.EnableTexture(texture.Id);

                if ((f & Faces.Front) != 0)
                {
                    // Front Face
                    // Normal Pointing Towards Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);
                }

                if ((f & Faces.Back) != 0)
                {
                    // Back Face
                    // Normal Pointing Away From Viewer
                    Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);
                }

                if ((f & Faces.Top) != 0)
                {
                    // Top Face
                    // Normal Pointing Up
                    Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, tile ? -length / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);
                }

                if ((f & Faces.Bottom) != 0)
                {
                    // Bottom Face
                    // Normal Pointing Down
                    Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, tile ? -length / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -length / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? width / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);
                }

                if ((f & Faces.Right) != 0)
                {
                    // Right face
                    // Normal Pointing Right
                    Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, -length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length / 2 : 1.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, -length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(width / 2, height / 2, length / 2);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(width / 2, -height / 2, length / 2);
                }

                if ((f & Faces.Left) != 0)
                {
                    // Left Face
                    // Normal Pointing Left
                    Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

                    // Bottom Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, -length / 2);

                    // Bottom Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length / 2 : 1.0f, 0.0f);
                    Rlgl.Vertex3f(-width / 2, -height / 2, length / 2);

                    // Top Right Of The Texture and Quad
                    Rlgl.TexCoord2f(tile ? length / 2 : 1.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, length / 2);

                    // Top Left Of The Texture and Quad
                    Rlgl.TexCoord2f(0.0f, tile ? -height / 2 : -1.0f);
                    Rlgl.Vertex3f(-width / 2, height / 2, -length / 2);
                }

                Rlgl.End();

                Rlgl.DisableTexture();
            }
        }
        // ripped straight from raylib
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CustomDrawMeshInstanced(Mesh mesh, Span<PartRenderInstanceInfo> transforms,
            WorkspaceRendererViewport viewport, PartSpecification partSpecification, bool useSurfaces, bool shadowPass = false)
        {
            int instances = transforms.Length;
            uint instancesVboId = 0;

            Shader shader = shadowPass ? viewport.ShadowMapShader : viewport.SpecularLightingInstancedShader;
            if (!shadowPass)
                viewport.ApplyPartSurfaceMaterial(partSpecification, useSurfaces);
            Rlgl.EnableShader(shader.Id);

            // Get a copy of current matrices to work with,
            // in case stereo render is required, and they need to be modified
            // NOTE: At this point the modelview matrix contains the view matrix (camera)
            // That's because BeginMode3D() sets it and there is no model-drawing function
            // that modifies it, all use rlPushMatrix() and rlPopMatrix()
            Matrix4x4 matModel = Raymath.MatrixIdentity();
            Matrix4x4 matView = Rlgl.GetMatrixModelview();
            Matrix4x4 matModelView = Raymath.MatrixIdentity();
            Matrix4x4 matProjection = Rlgl.GetMatrixProjection();

            // Upload view and projection matrices (if locations available)
            if (shader.Locs[(int)ShaderLocationIndex.MatrixView] != -1)
                Rlgl.SetUniformMatrix(shader.Locs[(int)ShaderLocationIndex.MatrixView], matView);
            if (shader.Locs[(int)ShaderLocationIndex.MatrixProjection] != -1)
                Rlgl.SetUniformMatrix(shader.Locs[(int)ShaderLocationIndex.MatrixProjection], matProjection);

            // Enable mesh VAO to attach new buffer
            Rlgl.EnableVertexArray(mesh.VaoId);

            // This could alternatively use a static VBO and either glMapBuffer() or glBufferSubData()
            // It isn't clear which would be reliably faster in all cases and on all platforms,
            // anecdotally glMapBuffer() seems quite slow (syncs) while glBufferSubData() seems
            // no faster, since all the transform matrices are transferred anyway
            fixed (void* instanceTransformPtr = transforms)
                instancesVboId = Rlgl.LoadVertexBuffer(instanceTransformPtr, instances * sizeof(PartRenderInstanceInfo), false);

            // Instances transformation matrices are sent to shader attribute location: SHADER_LOC_VERTEX_INSTANCETRANSFORM
            if (shader.Locs[(int)ShaderLocationIndex.VertexInstanceTransform] != -1)
            {
                for (uint i = 0; i < 4; i++)
                {
                    Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexInstanceTransform] + i);
                    Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexInstanceTransform] + i, 4,
                        Rlgl.FLOAT, 0, sizeof(PartRenderInstanceInfo), (int)(i * sizeof(Vector4)));
                    Rlgl.SetVertexAttributeDivisor((uint)shader.Locs[(int)ShaderLocationIndex.VertexInstanceTransform] + i, 1);
                }
            }

            int instanceColorLocation = shader.Locs[(int)ShaderLocationIndex.VertexColor];
            if (instanceColorLocation != -1)
            {
                Rlgl.EnableVertexAttribute((uint)instanceColorLocation);
                Rlgl.SetVertexAttribute((uint)instanceColorLocation, 4, Rlgl.FLOAT, 0,
                    sizeof(PartRenderInstanceInfo), sizeof(Float16));
                Rlgl.SetVertexAttributeDivisor((uint)instanceColorLocation, 1);
            }

            Rlgl.DisableVertexBuffer();
            Rlgl.DisableVertexArray();

            // Accumulate internal matrix transform (push/pop) and view matrix
            // NOTE: In this case, model instance transformation must be computed in the shader
            matModelView = Raymath.MatrixMultiply(Rlgl.GetMatrixTransform(), matView);

            // Upload model normal matrix (if locations available)
            if (shader.Locs[(int)ShaderLocationIndex.MatrixNormal] != -1)
                Rlgl.SetUniformMatrix(shader.Locs[(int)ShaderLocationIndex.MatrixNormal],
                    Raymath.MatrixTranspose(Raymath.MatrixInvert(matModel)));
            //-----------------------------------------------------

            // Try binding vertex array objects (VAO)
            // or use VBOs if not possible
            if (!Rlgl.EnableVertexArray(mesh.VaoId))
            {
                // Bind mesh VBO data: vertex position (shader-location = 0)
                Rlgl.EnableVertexBuffer(mesh.VboId[0]);
                Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexPosition], 3, Rlgl.FLOAT, 0, 0, 0);
                Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexPosition]);

                // Bind mesh VBO data: vertex texcoords (shader-location = 1)
                Rlgl.EnableVertexBuffer(mesh.VboId[1]);
                Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTexcoord01], 2, Rlgl.FLOAT, 0, 0, 0);
                Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTexcoord01]);

                if (shader.Locs[(int)ShaderLocationIndex.VertexNormal] != -1)
                {
                    // Bind mesh VBO data: vertex normals (shader-location = 2)
                    Rlgl.EnableVertexBuffer(mesh.VboId[2]);
                    Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexNormal], 3, Rlgl.FLOAT, 0, 0, 0);
                    Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexNormal]);
                }

                // Bind mesh VBO data: vertex colors (shader-location = 3, if available)
                if (shader.Locs[(int)ShaderLocationIndex.VertexColor] != -1)
                {
                    Rlgl.EnableVertexBuffer(mesh.VboId[3]);
                    Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexColor], 4, Rlgl.UNSIGNED_BYTE, 1, 0, 0);
                    Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexColor]);
                }

                // Bind mesh VBO data: vertex tangents (shader-location = 4, if available)
                if (shader.Locs[(int)ShaderLocationIndex.VertexTangent] != -1)
                {
                    Rlgl.EnableVertexBuffer(mesh.VboId[4]);
                    Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTangent], 4, Rlgl.FLOAT, 0, 0, 0);
                    Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTangent]);
                }

                // Bind mesh VBO data: vertex texcoords2 (shader-location = 5, if available)
                if (shader.Locs[(int)ShaderLocationIndex.VertexTexcoord02] != -1)
                {
                    Rlgl.EnableVertexBuffer(mesh.VboId[5]);
                    Rlgl.SetVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTexcoord02], 2, Rlgl.FLOAT, 0, 0, 0);
                    Rlgl.EnableVertexAttribute((uint)shader.Locs[(int)ShaderLocationIndex.VertexTexcoord02]);
                }

                if (mesh.Indices != default) Rlgl.EnableVertexBufferElement(mesh.VboId[6]);
            }

            int eyeCount = 1;
            if (Rlgl.IsStereoRenderEnabled()) eyeCount = 2;

            for (int eye = 0; eye < eyeCount; eye++)
            {
                // Calculate model-view-projection matrix (MVP)
                Matrix4x4 matModelViewProjection = Raymath.MatrixIdentity();
                if (eyeCount == 1) matModelViewProjection = Raymath.MatrixMultiply(matModelView, matProjection);
                else
                {
                    // Setup current eye viewport (half screen width)
                    Rlgl.Viewport(eye * Rlgl.GetFramebufferWidth() / 2, 0, Rlgl.GetFramebufferWidth() / 2, Rlgl.GetFramebufferHeight());
                    matModelViewProjection =
                        Raymath.MatrixMultiply(
                            Raymath.MatrixMultiply(matModelView, Rlgl.GetMatrixViewOffsetStereo(eye)), Rlgl.GetMatrixProjectionStereo(eye));
                }

                // Send combined model-view-projection matrix to shader
                Rlgl.SetUniformMatrix(shader.Locs[(int)ShaderLocationIndex.MatrixMvp], matModelViewProjection);

                // Draw mesh instanced
                if (mesh.Indices != default) Rlgl.DrawVertexArrayElementsInstanced(0, mesh.TriangleCount * 3, default, instances);
                else Rlgl.DrawVertexArrayInstanced(0, mesh.VertexCount, instances);
            }

            // Disable all possible vertex array objects (or VBOs)
            Rlgl.DisableVertexArray();
            Rlgl.DisableVertexBuffer();
            Rlgl.DisableVertexBufferElement();

            // Remove instance transforms buffer
            Rlgl.UnloadVertexBuffer(instancesVboId);

            Rlgl.DisableShader();
        }
    }
}
