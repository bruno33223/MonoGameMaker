using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Simulation;

namespace TestConsole.Tests.RTS
{
    public static class RTSPhysicsAndSeparationTests
    {
        public static void RunAll(ref int passCount, ref int totalCount, out long allocatedBytes)
        {
            Console.WriteLine("\n[SUÍTE RTS 3/6] Executando RTSPhysicsAndSeparationTests (TEST-PHYS-01 e 02)...");
            allocatedBytes = 0;

            Test_TEST_PHYS_01(ref passCount, ref totalCount);
            Test_TEST_PHYS_02(ref passCount, ref totalCount, out allocatedBytes);
        }

        /// <summary>
        /// TEST-PHYS-01: Repulsão suave mútua (Reynolds 1/d^2 com epsilon anti-singularidade).
        /// </summary>
        private static void Test_TEST_PHYS_01(ref int passCount, ref int totalCount)
        {
            totalCount++;

            Vector2 center = new Vector2(100f, 100f);
            float d1 = 20.0f;
            float d2 = 10.0f;

            // Teste A: Unidades a 20px de distância
            Vector2 posA = center;
            Vector2 posB = center + new Vector2(d1, 0f);
            Vector2 forceA = RtsPhysics.CalculateReynoldsRepulsion(posA, posB);
            float magA = forceA.Length();

            // Teste B: Unidades a 10px de distância
            Vector2 posC = center;
            Vector2 posD = center + new Vector2(d2, 0f);
            Vector2 forceB = RtsPhysics.CalculateReynoldsRepulsion(posC, posD);
            float magB = forceB.Length();

            // A razão deve ser aproximadamente 4.0 (com tolerância de 5% devido ao epsilon de 0.001)
            float ratio = magB / magA;
            float expectedRatio = 4.0f;
            float diff = MathF.Abs(ratio - expectedRatio);

            if (diff > 0.20f) // 5% de 4.0 é 0.20
            {
                throw new Exception($"[TEST-PHYS-01] Falha na lei do inverso do quadrado: magB/magA = {ratio:F4}, esperado {expectedRatio} (diff={diff:F4})");
            }

            // Direção deve apontar para longe de posB (-1, 0)
            Vector2 expectedDir = new Vector2(-1f, 0f);
            Vector2 actualDir = Vector2.Normalize(forceA);
            if (!RtsTestMath.ApproximatelyEqual(actualDir, expectedDir, 1e-4f))
            {
                throw new Exception($"[TEST-PHYS-01] Direção de repulsão incorreta: esperado {expectedDir}, obtido {actualDir}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-PHYS-01] Repulsão suave mútua (Reynolds 1/d^2 com epsilon anti-singularidade): PASS");
        }

        /// <summary>
        /// TEST-PHYS-02: Circle-Circle Hard Push impedindo sobreposição mútua (d >= 2R).
        /// </summary>
        private static void Test_TEST_PHYS_02(ref int passCount, ref int totalCount, out long deltaBytes)
        {
            totalCount++;
            const int unitCount = 50;
            const float unitRadius = 10.0f;
            float minAllowedDist = (unitRadius + unitRadius) - 1e-4f;

            var simSystem = new RtsSimulationSystem(unitCount, 2048, 2048, cellSize: 32f);

            // Posicionar forçadamente 50 unidades sobrepostas em um raio minúsculo de 5px em (500, 500)
            var rnd = new Random(12345);
            for (int i = 0; i < unitCount; i++)
            {
                float angle = (float)rnd.NextDouble() * MathF.PI * 2f;
                float r = (float)rnd.NextDouble() * 5.0f; // Todos dentro de 5px (severamente colapsados!)
                Vector2 spawnPos = new Vector2(500f + MathF.Cos(angle) * r, 500f + MathF.Sin(angle) * r);

                var data = new RtsUnitPhysicsData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Radius = unitRadius,
                    MaxSpeed = 100f,
                    MaxForce = 150f,
                    Mass = 1.0f,
                    TurnSpeed = 10f,
                    IsActive = 1,
                    TeamId = 1
                };
                simSystem.SetUnit(i, in data);
            }

            // Aquecimento
            simSystem.Update(0.016f);

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            // Executar múltiplos passos de simulação e relaxamento físico
            for (int step = 0; step < 25; step++)
            {
                simSystem.Update(0.016f);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            deltaBytes = bytesAfter - bytesBefore;

            if (deltaBytes != 0)
            {
                throw new Exception($"[TEST-PHYS-02] Falha Zero-GC na física: alocou {deltaBytes} bytes durante relaxamento físico!");
            }

            // Varrer todas as unidades e garantir que d >= 2R para TODOS os pares
            for (int i = 0; i < unitCount; i++)
            {
                Vector2 posA = simSystem.GetUnitPosition(i);
                for (int j = i + 1; j < unitCount; j++)
                {
                    Vector2 posB = simSystem.GetUnitPosition(j);
                    float dist = Vector2.Distance(posA, posB);

                    if (dist < minAllowedDist)
                    {
                        throw new Exception($"[TEST-PHYS-02] Interpenetração física residual entre unidade {i} e {j}: dist={dist:F4} < minDist={minAllowedDist:F4}");
                    }
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-PHYS-02] Circle-Circle Hard Push impedindo sobreposição mútua (d >= 2R): PASS");
        }
    }
}
