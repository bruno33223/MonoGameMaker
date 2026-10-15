using System;

namespace TestConsole.Tests.Domain
{
    /// <summary>
    /// Motor matemático canônico de progressão, escalonamento e curvas de almas.
    /// </summary>
    public static class ProgressionMath
    {
        public static float CalculateHP(float baseHp, int stageLevel)
        {
            float factor = MathF.Max(0f, 1f + 0.15f * (stageLevel - 1));
            return baseHp * MathF.Pow(factor, 1.1f);
        }

        public static float CalculateDamage(float baseDamage, int stageLevel)
        {
            float factor = MathF.Max(0f, 1f + 0.10f * (stageLevel - 1));
            return baseDamage * factor;
        }

        public static long SoulsRequired(int level)
        {
            long lvl = Math.Max(0L, (long)level);
            if (lvl <= 10L)
            {
                return (15L * lvl * lvl) + (45L * lvl) + 40L;
            }

            // Transição C1-contínua em level = 10: base 1990, inclinação 345, termo quadrático 5
            const long baseCost10 = 1990L;
            long delta = lvl - 10L;
            return baseCost10 + (345L * delta) + (5L * delta * delta);
        }
    }
}
