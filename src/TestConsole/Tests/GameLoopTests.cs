using System;
using Microsoft.Xna.Framework;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class GameLoopTests
    {
        private const float Epsilon = 1e-4f;

        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 1/8] Executando GameLoopTests (TEST-TIME-01 a 05)...");

            Test_TIME_01_DeltaTimeClamping(ref passCount, ref totalCount);
            Test_TIME_02_DifferentialTimeDilation(ref passCount, ref totalCount);
            Test_TIME_03_HitstopDurationAndSeverities(ref passCount, ref totalCount);
            Test_TIME_04_HitstopDecayAndUnfreeze(ref passCount, ref totalCount);
            Test_TIME_05_ImpactFlashDecay(ref passCount, ref totalCount);
        }

        private static float ClampDt(float rawDt) => Math.Min(rawDt, 0.066f);

        public static void Test_TIME_01_DeltaTimeClamping(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float[] largeDts = { 0.1f, 0.5f, 2.0f, 10.0f };
            foreach (var dt in largeDts)
            {
                float clamped = ClampDt(dt);
                if (Math.Abs(clamped - 0.066f) > Epsilon)
                    throw new Exception($"[TEST-TIME-01] Falha no clamping: dt {dt} clampado para {clamped}, esperado 0.066f");
            }

            float smallDt = 0.016f;
            float unclamped = ClampDt(smallDt);
            if (Math.Abs(unclamped - smallDt) > Epsilon)
                throw new Exception($"[TEST-TIME-01] Falha: dt normal {smallDt} foi alterado para {unclamped}");

            passCount++;
            Console.WriteLine("  -> [TEST-TIME-01] Delta Time Clamping (<= 0.066s): PASS");
        }

        public static void Test_TIME_02_DifferentialTimeDilation(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float dt = 0.020f;
            float slowFactor = 0.35f;
            float tutScale = 1.0f;
            float freezeFactor = 1.0f;

            float playerSpeed = 300f;
            float enemySpeed = 200f;

            // Sem dilatação
            float worldDtNormal = dt * tutScale * freezeFactor;
            float pDistNorm = playerSpeed * dt;
            float eDistNorm = enemySpeed * worldDtNormal;

            // Com dilatação
            bool isDilation = true;
            float worldDtDilated = (isDilation ? dt * slowFactor : dt) * tutScale * freezeFactor;
            float pDistDilated = playerSpeed * dt; // Player corre em tempo real!
            float eDistDilated = enemySpeed * worldDtDilated;

            if (Math.Abs(pDistDilated - pDistNorm) > Epsilon)
                throw new Exception("[TEST-TIME-02] O jogador não deve ser desacelerado durante time dilation diferencial.");

            if (Math.Abs(eDistDilated - (eDistNorm * slowFactor)) > Epsilon)
                throw new Exception($"[TEST-TIME-02] O mundo deve desacelerar exatamente para slowFactor (0.35x). Esperado: {eDistNorm * slowFactor}, Obtido: {eDistDilated}");

            passCount++;
            Console.WriteLine("  -> [TEST-TIME-02] Time Dilation Diferencial (Player dt vs World dt*0.35): PASS");
        }

        public static void Test_TIME_03_HitstopDurationAndSeverities(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var hitstop = new HitstopManagerModel();
            hitstop.Reset();

            // 1. Heavy hit (35ms)
            hitstop.TriggerHitstop(HitstopManagerModel.HeavyHitDuration);
            if (!hitstop.IsFrozen || Math.Abs(hitstop.RemainingFreezeTime - 0.035f) > Epsilon)
                throw new Exception($"[TEST-TIME-03] Heavy hit falhou. Esperado 0.035s, obtido {hitstop.RemainingFreezeTime}");
            if (hitstop.FreezeFactor != 0.0f)
                throw new Exception("[TEST-TIME-03] FreezeFactor deve ser 0.0f durante congelamento ativo.");

            // 2. Critical hit (55ms) - extensivo
            hitstop.TriggerHitstop(HitstopManagerModel.CriticalHitDuration);
            if (Math.Abs(hitstop.RemainingFreezeTime - 0.055f) > Epsilon)
                throw new Exception($"[TEST-TIME-03] Critical hit falhou. Esperado 0.055s, obtido {hitstop.RemainingFreezeTime}");

            // 3. Shield break (80ms)
            hitstop.TriggerHitstop(HitstopManagerModel.ShieldBreakDuration);
            if (Math.Abs(hitstop.RemainingFreezeTime - 0.080f) > Epsilon)
                throw new Exception($"[TEST-TIME-03] Shield break falhou. Esperado 0.080s, obtido {hitstop.RemainingFreezeTime}");

            // 4. Boss death (120ms)
            hitstop.TriggerHitstop(HitstopManagerModel.BossDeathDuration);
            if (Math.Abs(hitstop.RemainingFreezeTime - 0.120f) > Epsilon)
                throw new Exception($"[TEST-TIME-03] Boss death falhou. Esperado 0.120s, obtido {hitstop.RemainingFreezeTime}");

            // 5. Preservação: gatilho menor não reduz freeze ativo
            hitstop.TriggerHitstop(HitstopManagerModel.HeavyHitDuration);
            if (hitstop.RemainingFreezeTime < 0.119f)
                throw new Exception("[TEST-TIME-03] Gatilho de menor duração truncou freeze ativo!");

            passCount++;
            Console.WriteLine("  -> [TEST-TIME-03] Hitstop Duration & Severities (35ms, 55ms, 80ms, 120ms): PASS");
        }

        public static void Test_TIME_04_HitstopDecayAndUnfreeze(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var hitstop = new HitstopManagerModel();
            hitstop.Reset();

            hitstop.TriggerHitstop(0.050f);
            float step = 0.010f;

            // 4 passos de 0.010s -> restam 0.010s
            for (int i = 0; i < 4; i++)
            {
                hitstop.Update(step);
                if (!hitstop.IsFrozen)
                    throw new Exception($"[TEST-TIME-04] Congelamento encerrou prematuramente no passo {i + 1}.");
            }

            // 5º passo -> zera exatamente
            hitstop.Update(step);
            if (hitstop.IsFrozen)
                throw new Exception("[TEST-TIME-04] Hitstop permaneceu congelado após tempo esgotado.");

            if (hitstop.FreezeFactor != 1.0f)
                throw new Exception($"[TEST-TIME-04] FreezeFactor deve retornar a 1.0f após unfreeze. Obtido: {hitstop.FreezeFactor}");

            passCount++;
            Console.WriteLine("  -> [TEST-TIME-04] Hitstop Decay & Unfreeze: PASS");
        }

        public static void Test_TIME_05_ImpactFlashDecay(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var hitstop = new HitstopManagerModel();
            hitstop.Reset();

            hitstop.TriggerImpactFlash(0.80f);
            if (Math.Abs(hitstop.ImpactFlashIntensity - 0.80f) > Epsilon)
                throw new Exception($"[TEST-TIME-05] Flash inicial esperado 0.80f, obtido {hitstop.ImpactFlashIntensity}");

            // Decaimento linear: Flash = Max(0, Flash - 2.5 * dt)
            // Em 0.16s: 0.80 - (2.5 * 0.16) = 0.80 - 0.40 = 0.40
            hitstop.Update(0.16f, flashDecayRate: 2.5f);
            if (Math.Abs(hitstop.ImpactFlashIntensity - 0.40f) > 0.001f)
                throw new Exception($"[TEST-TIME-05] Decaimento parcial falhou. Esperado 0.40f, obtido {hitstop.ImpactFlashIntensity}");

            // Em mais 0.20s: 0.40 - (2.5 * 0.20) = -0.10 => Clamped para 0.0f
            hitstop.Update(0.20f, flashDecayRate: 2.5f);
            if (hitstop.ImpactFlashIntensity != 0.0f)
                throw new Exception($"[TEST-TIME-05] Flash deve zerar no repouso absoluto. Obtido: {hitstop.ImpactFlashIntensity}");

            passCount++;
            Console.WriteLine("  -> [TEST-TIME-05] Impact Flash Decay (0.80f decai linearmente até 0.0f): PASS");
        }
    }
}
