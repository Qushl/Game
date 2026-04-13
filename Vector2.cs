using System;

namespace TopDownHighwayDrifter
{
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2(0, 0);

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }

        public Vector2 Normalize()
        {
            float len = Length();
            if (len == 0) return Zero;
            return new Vector2(X / len, Y / len);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new Vector2(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2 operator *(Vector2 v, float scalar)
        {
            return new Vector2(v.X * scalar, v.Y * scalar);
        }

        public float DotProduct(Vector2 other)
        {
            return X * other.X + Y * other.Y;
        }

        public float AngleTo(Vector2 other)
        {
            var normalized = other.Normalize();
            float dot = DotProduct(normalized) / Length();
            dot = Math.Clamp(dot, -1, 1);
            return (float)Math.Acos(dot);
        }
    }
}
