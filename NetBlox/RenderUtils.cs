using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox
{
	public static class RenderUtils
	{
		public static void DrawCubeTextureRec(Texture texture, Vector3 position, Vector3 rotation, float width, float height, float length, Color color, Faces f, bool tile = false)
		{
			RlGl.rlPushMatrix();
			RlGl.rlRotatef(rotation.X, 1, 0, 0);
			RlGl.rlRotatef(rotation.Y, 0, 1, 0);
			RlGl.rlRotatef(rotation.Z, 0, 0, 1);
			RlGl.rlTranslatef(position.X, position.Y, position.Z);
			// im not willing to rewrite the whole shit
			position = Vector3.Zero;

			if (f != 0)
			{
				float x = position.X;
				float y = position.Y;
				float z = position.Z;

				RlGl.rlSetTexture(texture.id);

				RlGl.rlBegin(7);
				RlGl.rlColor4ub(color.r, color.g, color.b, color.a);

				RlGl.rlTextureParameters(0, RlGl.RL_TEXTURE_WRAP_S, RlGl.RL_TEXTURE_WRAP_REPEAT);
				RlGl.rlTextureParameters(0, RlGl.RL_TEXTURE_WRAP_T, RlGl.RL_TEXTURE_WRAP_REPEAT);

				// NOTE: Enable texture 1 for Front, Back
				RlGl.rlEnableTexture(texture.id);

				if (f.HasFlag(Faces.Front))
				{
					// Front Face
					// Normal Pointing Towards Viewer
					RlGl.rlNormal3f(0.0f, 0.0f, 1.0f);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Back))
				{
					// Back Face
					// Normal Pointing Away From Viewer
					RlGl.rlNormal3f(0.0f, 0.0f, -1.0f);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Top))
				{
					// Top Face
					// Normal Pointing Up
					RlGl.rlNormal3f(0.0f, 1.0f, 0.0f);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -length : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Bottom))
				{
					// Bottom Face
					// Normal Pointing Down
					RlGl.rlNormal3f(0.0f, -1.0f, 0.0f);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, tile ? -length : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -length : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? width : 1.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Right))
				{
					// Right face
					// Normal Pointing Right
					RlGl.rlNormal3f(1.0f, 0.0f, 0.0f);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? length : 1.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Left))
				{
					// Left Face
					// Normal Pointing Left
					RlGl.rlNormal3f(-1.0f, 0.0f, 0.0f);

					// Bottom Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);

					// Bottom Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? length : 1.0f, 0.0f);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);

					// Top Right Of The Texture and Quad
					RlGl.rlTexCoord2f(tile ? length : 1.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);

					// Top Left Of The Texture and Quad
					RlGl.rlTexCoord2f(0.0f, tile ? -height : -1.0f);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);
				}

				RlGl.rlEnd();

				RlGl.rlDisableTexture();
			}

			RlGl.rlPopMatrix();
		}
		public static void DrawCubeFaced(Vector3 position, Vector3 rotation, float width, float height, float length, Color color, Faces f)
		{
			RlGl.rlPushMatrix();
			RlGl.rlTranslatef(position.X, position.Y, position.Z);
			RlGl.rlRotatef(rotation.X, 1, 0, 0);
			RlGl.rlRotatef(rotation.Y, 0, 1, 0);
			RlGl.rlRotatef(rotation.Z, 0, 0, 1);
			// im not willing to rewrite the whole shit
			position = Vector3.Zero;

			if (f != 0)
			{
				float x = position.X;
				float y = position.Y;
				float z = position.Z;

				RlGl.rlBegin(7);
				RlGl.rlColor4ub(color.r, color.g, color.b, color.a);

				if (f.HasFlag(Faces.Front))
				{
					RlGl.rlNormal3f(0.0f, 0.0f, 1.0f);

					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Back))
				{
					RlGl.rlNormal3f(0.0f, 0.0f, -1.0f);

					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Top))
				{
					RlGl.rlNormal3f(0.0f, 1.0f, 0.0f);

					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);
				}

				if (f.HasFlag(Faces.Bottom))
				{
					RlGl.rlNormal3f(0.0f, -1.0f, 0.0f);

					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Right))
				{
					RlGl.rlNormal3f(1.0f, 0.0f, 0.0f);

					RlGl.rlVertex3f(x + width / 2, y - height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z - length / 2);
					RlGl.rlVertex3f(x + width / 2, y + height / 2, z + length / 2);
					RlGl.rlVertex3f(x + width / 2, y - height / 2, z + length / 2);
				}

				if (f.HasFlag(Faces.Left))
				{
					RlGl.rlNormal3f(-1.0f, 0.0f, 0.0f);

					RlGl.rlVertex3f(x - width / 2, y - height / 2, z - length / 2);
					RlGl.rlVertex3f(x - width / 2, y - height / 2, z + length / 2);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z + length / 2);
					RlGl.rlVertex3f(x - width / 2, y + height / 2, z - length / 2);
				}

				RlGl.rlEnd();
			}

			RlGl.rlPopMatrix();
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
