using System;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Spatial
{
    /// <summary>
    /// Representa uma caixa delimitadora 2D imutável de ponto flutuante alinhada aos eixos para consultas espaciais de mundo.
    /// </summary>
    public readonly struct RectangleF : IEquatable<RectangleF>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;
        public Vector2 Min => new Vector2(X, Y);
        public Vector2 Max => new Vector2(Right, Bottom);

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleF(Vector2 min, Vector2 max)
        {
            X = MathF.Min(min.X, max.X);
            Y = MathF.Min(min.Y, max.Y);
            Width = MathF.Abs(max.X - min.X);
            Height = MathF.Abs(max.Y - min.Y);
        }

        public bool Contains(Vector2 point) =>
            point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

        public bool Contains(float x, float y) =>
            x >= Left && x <= Right && y >= Top && y <= Bottom;

        public bool Contains(RectangleF other) =>
            other.Left >= Left && other.Right <= Right && other.Top >= Top && other.Bottom <= Bottom;

        public bool Intersects(RectangleF other) =>
            Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

        public bool Equals(RectangleF other) =>
            X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public override bool Equals(object? obj) =>
            obj is RectangleF other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(RectangleF left, RectangleF right) =>
            left.Equals(right);

        public static bool operator !=(RectangleF left, RectangleF right) =>
            !left.Equals(right);

        public override string ToString() =>
            $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";
    }
}
