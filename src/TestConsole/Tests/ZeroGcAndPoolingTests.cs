using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pooling;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class ZeroGcAndPoolingTests
    {
        public static void RunAll(ref int passCount, ref int totalCount, out long combatAllocatedBytes)
        {
            Console.WriteLine("\n[SUÍTE 8/8] Executando ZeroGcAndPoolingTests (TEST-POL-01 a 04)...");

            combatAllocatedBytes = 0;
            Test_POL_01_ProjectilePoolRecycling(ref passCount, ref totalCount);
            Test_POL_02_SpatialSoulCoalescence(ref passCount, ref totalCount);
            Test_POL_03_PoolSaturationForcedFusion(ref passCount, ref totalCount);
            Test_POL_04_ZeroGcFullCombatSimulation(ref passCount, ref totalCount, out combatAllocatedBytes);
        }

        public static void Test_POL_01_ProjectilePoolRecycling(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var pool = new ObjectPool<ProjectileModel>(() => new ProjectileModel(), initialCapacity: 512);
            pool.PreWarm(512);

            if (pool.Count != 512)
                throw new Exception($"[TEST-POL-01] PreWarm falhou: esperado 512, obtido {pool.Count}");

            // Força coleta de GC antes do loop de disparos
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            long memBefore = GC.GetAllocatedBytesForCurrentThread();

            // Dispara e recicla 10.000 projéteis
            for (int i = 0; i < 10000; i++)
            {
                var p = pool.Get();
                p.Position = new Vector2(i, i);
                p.Velocity = new Vector2(100f, 0f);

                if (!p.IsActive)
                    throw new Exception("[TEST-POL-01] Projétil resgatado deve estar IsActive = true.");

                pool.Release(p);

                if (p.IsActive || p.Position != Vector2.Zero || p.Damage != 0f)
                    throw new Exception("[TEST-POL-01] Projétil liberado não foi resetado limpo em OnDespawn.");
            }

            long memAfter = GC.GetAllocatedBytesForCurrentThread();
            long delta = memAfter - memBefore;

            if (delta > 0)
                throw new Exception($"[TEST-POL-01] Alocação indevida detectada durante reciclagem de projéteis: {delta} bytes");

            passCount++;
            Console.WriteLine("  -> [TEST-POL-01] Reciclagem de Projéteis (ObjectPool<T> 512 pré-aquecidos, 10.000 ciclos Zero-GC): PASS");
        }

        public static void Test_POL_02_SpatialSoulCoalescence(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soulMgr = new SoulManagerModel();

            // Duas almas valor 5 a uma distância de 20px (<= 32px MergeRadius)
            var s1 = soulMgr.SpawnSoul(new Vector2(0f, 0f), 5);
            var s2 = soulMgr.SpawnSoul(new Vector2(20f, 0f), 5);

            if (s1 == null || s2 == null)
                throw new Exception("[TEST-POL-02] SpawnSoul retornou nulo.");

            if (soulMgr.ActiveCount != 2)
                throw new Exception($"[TEST-POL-02] ActiveCount inicial esperado 2, obtido {soulMgr.ActiveCount}");

            soulMgr.UpdateSpatialCoalescence();

            if (soulMgr.ActiveCount != 1)
                throw new Exception($"[TEST-POL-02] Fusão falhou: ActiveCount pós-fusão esperado 1, obtido {soulMgr.ActiveCount}");

            if (s1.Value != 10)
                throw new Exception($"[TEST-POL-02] Valor da alma fundida esperado 10, obtido {s1.Value}");

            if (s2.IsActive)
                throw new Exception("[TEST-POL-02] Segunda alma absorvida deve estar desativada (IsActive = false).");

            passCount++;
            Console.WriteLine("  -> [TEST-POL-02] Fusão Espacial de Núcleos de Alma (< 32px): PASS");
        }

        public static void Test_POL_03_PoolSaturationForcedFusion(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soulMgr = new SoulManagerModel();

            // Preenche todo o pool com 128 almas em posições espaçadas (> 32px)
            for (int i = 0; i < SoulManagerModel.PoolCapacity; i++)
            {
                soulMgr.SpawnSoul(new Vector2(i * 100f, 0f), 10);
            }

            if (soulMgr.ActiveCount != 128)
                throw new Exception($"[TEST-POL-03] Pool não foi totalmente preenchido: {soulMgr.ActiveCount}/128");

            // Tenta spawnar a 129ª alma próxima da alma 0 (posição (5, 0)) com valor 50
            var merged = soulMgr.SpawnSoul(new Vector2(5f, 0f), 50);

            // O pool deve permanecer rigorosamente em 128 instâncias ativas
            if (soulMgr.ActiveCount != 128)
                throw new Exception($"[TEST-POL-03] Teto do pool violado: {soulMgr.ActiveCount} almas ativas (máximo 128)");

            // A alma 0 deve ter absorvido os 50 de valor (10 + 50 = 60)
            if (soulMgr.Pool[0].Value != 60)
                throw new Exception($"[TEST-POL-03] Fusão forçada na alma mais próxima falhou. Esperado 60, obtido {soulMgr.Pool[0].Value}");

            passCount++;
            Console.WriteLine("  -> [TEST-POL-03] Saturação de Pool e Fusão Forçada (Teto estrito de 128 Almas): PASS");
        }

        public static void Test_POL_04_ZeroGcFullCombatSimulation(ref int passCount, ref int totalCount, out long combatAllocatedBytes)
        {
            totalCount++;

            // Configuração do ambiente simulado de combate
            var pool = new ObjectPool<ProjectileModel>(() => new ProjectileModel(), initialCapacity: 512);
            pool.PreWarm(512);

            var enemyStats = new StatProfile(100f, 50f, 20f);
            var enemyHealth = new HealthController(enemyStats);

            var playerStats = new StatProfile(100f, 50f, 0f);
            var playerHealth = new HealthController(playerStats);

            var hitstop = new HitstopManagerModel();
            var dmg = DamageInfo.FromRaw(25f, DamageType.Physical);

            // Pré-aquecimento do JIT e estruturas
            for (int i = 0; i < 100; i++)
            {
                var p = pool.Get();
                playerHealth.TakeDamage(dmg, out _, out _);
                hitstop.TriggerHitstop(0.035f);
                hitstop.Update(0.016f);
                pool.Release(p);
            }

            // Limpeza forçada do coletor de lixo
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            long memBefore = GC.GetAllocatedBytesForCurrentThread();

            // 10.000 iterações completas do loop de combate
            for (int i = 0; i < 10000; i++)
            {
                var proj = pool.Get();
                proj.Position = new Vector2(i, 0);

                // Aplica dano com mitigação e cálculo de armadura
                enemyHealth.TakeDamage(dmg, out float taken, out bool absorbed);

                // Atualiza hitstop
                hitstop.Update(0.016f);

                // Devolve projétil
                pool.Release(proj);

                if (enemyHealth.IsDead)
                {
                    enemyHealth.ResetToMax();
                }
            }

            long memAfter = GC.GetAllocatedBytesForCurrentThread();
            combatAllocatedBytes = memAfter - memBefore;

            if (combatAllocatedBytes > 0)
            {
                throw new InvalidOperationException(
                    $"[FALHA ZERO-GC] O loop de simulação de combate alocou {combatAllocatedBytes} bytes no Heap! Esperado: 0 bytes.");
            }

            passCount++;
            Console.WriteLine($"  -> [TEST-POL-04] Ausência de Alocações no Loop de Combate Completo (10.000 Ciclos, 0 bytes alocados): PASS");
        }
    }
}
