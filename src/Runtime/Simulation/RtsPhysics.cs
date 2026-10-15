using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Simulation
{
    /// <summary>
    /// Algoritmos matemáticos de física RTS: repulsão suave de Reynolds (1/d^2) e relaxamento rígido (Hard Push).
    /// </summary>
    public static class RtsPhysics
    {
        public const float EpsilonAntiSingularity = 0.001f;

        /// <summary>
        /// Calcula a força de repulsão suave de Reynolds entre duas unidades: F = (dir) * (k / (d^2 + epsilon)).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 CalculateReynoldsRepulsion(
            Vector2 posA,
            Vector2 posB,
            float weight = 45.0f,
            float epsilon = EpsilonAntiSingularity)
        {
            Vector2 diff = posA - posB;
            float distSq = diff.LengthSquared();

            if (distSq <= 0.0001f)
            {
                return Vector2.Zero;
            }

            float dist = MathF.Sqrt(distSq);
            Vector2 dir = diff / dist;
            float magnitude = weight / (distSq + epsilon);

            return dir * magnitude;
        }

        /// <summary>
        /// Aplica o relaxamento posicional direto (Circle-Circle Hard Push Constraint) entre duas unidades.
        /// Garante d >= R1 + R2 projetando 50% da penetração ao longo da normal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ResolveHardPush(
            ref Vector2 posA,
            ref Vector2 posB,
            float radiusA,
            float radiusB,
            int idA = 0,
            int idB = 1)
        {
            Vector2 delta = posA - posB;
            float dist = delta.Length();
            float minDist = radiusA + radiusB;

            if (dist < minDist)
            {
                Vector2 normal;
                if (dist > 0.0001f)
                {
                    normal = delta / dist;
                }
                else
                {
                    float jx = ((idA % 3) - 1);
                    float jy = ((idB % 3) - 1);
                    if (MathF.Abs(jx) < 0.01f && MathF.Abs(jy) < 0.01f) jx = 1.0f;
                    normal = Vector2.Normalize(new Vector2(jx, jy));
                }

                float overlap = (minDist - dist) + 0.005f;
                Vector2 push = normal * (overlap * 0.5f);

                posA += push;
                posB -= push;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executa relaxamento rígido iterativo em um grupo de unidades com posições e raios arbitrários.
        /// </summary>
        public static void RelaxHardPushBatch(Span<Vector2> positions, ReadOnlySpan<float> radii, int iterations = 10)
        {
            int n = positions.Length;
            for (int it = 0; it < iterations; it++)
            {
                bool anyOverlap = false;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        Vector2 posA = positions[i];
                        Vector2 posB = positions[j];
                        float rA = radii[i];
                        float rB = radii[j];

                        if (ResolveHardPush(ref posA, ref posB, rA, rB, i, j))
                        {
                            positions[i] = posA;
                            positions[j] = posB;
                            anyOverlap = true;
                        }
                    }
                }
                if (!anyOverlap) break;
            }
        }
    }
}
