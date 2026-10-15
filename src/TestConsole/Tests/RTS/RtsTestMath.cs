using System;
using Microsoft.Xna.Framework;

namespace TestConsole.Tests.RTS
{
    /// <summary>
    /// Tolerâncias numéricas e helpers de validação matemática estrita para testes RTS.
    /// </summary>
    public static class RtsTestMath
    {
        public const float EpsilonPosition = 1e-4f;
        public const float EpsilonVelocity = 1e-4f;
        public const float EpsilonAngle    = 1e-4f;
        public const float EpsilonNormal   = 1e-5f;

        public static bool ApproximatelyEqual(float a, float b, float epsilon = EpsilonPosition)
        {
            return MathF.Abs(a - b) <= epsilon;
        }

        public static bool ApproximatelyEqual(Vector2 a, Vector2 b, float epsilon = EpsilonPosition)
        {
            return (a - b).LengthSquared() <= (epsilon * epsilon);
        }
    }
}
