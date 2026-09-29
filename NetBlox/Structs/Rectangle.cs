using System.Numerics;

namespace NetBlox.Structs;

public record struct Rectangle
{
    public float X => Position.X;
    public float Y => Position.Y;
    public float Width => Size.X;
    public float Height => Size.Y;
    public Vector2 Position;
    public Vector2 Size;

    public Rectangle(int x, int y, int w, int h)
    {
        Position.X = x;
        Position.Y = y;
        Size.X = w;
        Size.Y = h;
    }
    public Rectangle(float x, float y, float w, float h)
    {
        Position.X = x;
        Position.Y = y;
        Size.X = w;
        Size.Y = h;
    }
    public Rectangle(Vector2 pos, Vector2 size)
    {
        Position = pos;
        Size = size;
    }

    public Rectangle IntersectWith(Rectangle rectangle)
    {
        float x1 = Math.Max(X, rectangle.X);
        float x2 = Math.Min(X + Width, rectangle.X + rectangle.Width);
        float y1 = Math.Max(Y, rectangle.Y);
        float y2 = Math.Min(Y + Height, rectangle.Y + rectangle.Height);

        if (x2 >= x1 && y2 >= y1)
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);

        return default;
    }
    public bool ContainsPoint(Vector2 vector2)
    {
        return vector2.X >= Position.X && vector2.Y >= Position.Y && 
            vector2.X <= (Position.X + Size.X) && vector2.Y <= (Position.Y + Size.Y);
    }
}