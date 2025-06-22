using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox
{
	public unsafe static class RenderUtils
	{
		public static void DrawCubeTextureRec(Texture2D texture, Vector3 position, Quaternion rotation, float width, float height, float length, Color color, Faces f, bool tile = false)
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
		public static void DrawCubeFaced(Vector3 position, Quaternion rotation, float width, float height, float length, Color color, Faces f)
		{
			Rlgl.PushMatrix();
			Rlgl.MatrixMode(MatrixMode.Texture);
			Rlgl.Translatef(position.X, position.Y, position.Z);
			Rlgl.MultMatrixf(Raymath.QuaternionToMatrix(rotation));
			// im not willing to rewrite the whole shit
			position = Vector3.Zero;

			if (f != 0)
			{
				float x = position.X;
				float y = position.Y;
				float z = position.Z;

				Rlgl.Begin(7);
				Rlgl.Color4ub(color.R, color.G, color.B, color.A);

				if ((f & Faces.Front) != 0)
				{
					Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
				}

				if ((f & Faces.Back) != 0)
				{
					Rlgl.Normal3f(0.0f, 0.0f, -1.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
				}

				if ((f & Faces.Top) != 0)
				{
					Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
				}

				if ((f & Faces.Bottom) != 0)
				{
					Rlgl.Normal3f(0.0f, -1.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
				}

				if ((f & Faces.Right) != 0)
				{
					Rlgl.Normal3f(1.0f, 0.0f, 0.0f);

					Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
					Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
				}

				if ((f & Faces.Left) != 0)
				{
					Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);

					Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
					Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
					Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
				}

				Rlgl.End();
			}

			Rlgl.PopMatrix();
		}
		public static float DistanceFrom(Vector3 vect, Vector3 vect2)
		{
			return MathF.Sqrt((vect.X - vect2.X) * (vect.X - vect2.X) +
					(vect.Y - vect2.Y) * (vect.Y - vect2.Y) +
					(vect.Z - vect2.Z) * (vect.Z - vect2.Z));
		}
		public static bool MouseCollides(Vector2 p, Vector2 s)
		{
			int mx = Raylib.GetMouseX();
			int my = Raylib.GetMouseY();
			return (p.X <= mx && p.X + s.X >= mx) && (p.Y <= my && p.Y + s.Y >= my);
		}
	}
}
