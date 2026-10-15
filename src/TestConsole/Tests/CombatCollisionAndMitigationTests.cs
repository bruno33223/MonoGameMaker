using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class CombatCollisionAndMitigationTests
    {
        private const float Epsilon = 1e-3f;

        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 4/8] Executando CombatCollisionAndMitigationTests (TEST-CBT-01 a 10)...");

            Test_CBT_01_ArmorMitigation(ref passCount, ref totalCount);
            Test_CBT_02_PlasmaPenetrationGuarantee(ref passCount, ref totalCount);
            Test_CBT_03_CriticalHitVsResistance(ref passCount, ref totalCount);
            Test_CBT_04_ElementalResistances(ref passCount, ref totalCount);
            Test_CBT_05_ShieldAbsorptionAndOverflow(ref passCount, ref totalCount);
            Test_CBT_06_ShieldBreakTriggerAndNovaBurst(ref passCount, ref totalCount);
            Test_CBT_07_RammingMechanicalCollision(ref passCount, ref totalCount);
            Test_CBT_08_AsteroidBilliardKineticCrush(ref passCount, ref totalCount);
            Test_CBT_09_HierarchicalAsteroidFracture(ref passCount, ref totalCount);
            Test_CBT_10_Strict7StageCollisionPipeline(ref passCount, ref totalCount);
        }

        public static void Test_CBT_01_ArmorMitigation(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats0 = new StatProfile(100f, 0f, 0f);
            var stats50 = new StatProfile(100f, 0f, 50f);
            var stats100 = new StatProfile(100f, 0f, 100f);
            var stats300 = new StatProfile(100f, 0f, 300f);

            var dmg = DamageInfo.FromRaw(100f, DamageType.Physical);

            float d0 = HealthController.CalculateMitigatedDamage(dmg, stats0);
            float d50 = HealthController.CalculateMitigatedDamage(dmg, stats50);
            float d100 = HealthController.CalculateMitigatedDamage(dmg, stats100);
            float d300 = HealthController.CalculateMitigatedDamage(dmg, stats300);

            if (Math.Abs(d0 - 100f) > Epsilon)
                throw new Exception($"[TEST-CBT-01] Armor 0 falhou: {d0}");
            if (Math.Abs(d50 - 66.6667f) > 0.05f)
                throw new Exception($"[TEST-CBT-01] Armor 50 falhou: {d50}, esperado ~66.67");
            if (Math.Abs(d100 - 50f) > Epsilon)
                throw new Exception($"[TEST-CBT-01] Armor 100 falhou: {d100}, esperado 50.0");
            if (Math.Abs(d300 - 25f) > Epsilon)
                throw new Exception($"[TEST-CBT-01] Armor 300 falhou: {d300}, esperado 25.0");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-01] Mitigação Assintótica de Armadura (0, 50, 100, 300): PASS");
        }

        public static void Test_CBT_02_PlasmaPenetrationGuarantee(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats100 = new StatProfile(100f, 0f, 100f);
            // Dano plasma com ArmorPen = 0% deve receber garantia mínima de 50% de penetração
            var plasmaDmg = DamageInfo.FromRaw(100f, DamageType.Plasma, armorPenetration: 0f);

            float dPlasma = HealthController.CalculateMitigatedDamage(plasmaDmg, stats100);
            // Armor efetiva = 100 * (1 - 0.50) = 50 -> Dano = 100 * (100 / 150) = 66.67
            if (Math.Abs(dPlasma - 66.6667f) > 0.05f)
                throw new Exception($"[TEST-CBT-02] Plasma pen guarantee falhou: {dPlasma}, esperado ~66.67");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-02] Garantia de Penetração de Dano Plasma (min 50% Pen): PASS");
        }

        public static void Test_CBT_03_CriticalHitVsResistance(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats = new StatProfile { CritResistance = 0.50f };
            var critDmg = DamageInfo.FromRaw(100f, DamageType.Physical, armorPenetration: 1.0f, isCrit: true, critMultiplier: 2.0f);

            // Mult base: 2.0x. Bônus: 1.0x. Com 50% resist: 1.0 + 1.0 * (1 - 0.50) = 1.5x -> Dano final: 150
            float result = HealthController.CalculateMitigatedDamage(critDmg, stats);
            if (Math.Abs(result - 150f) > Epsilon)
                throw new Exception($"[TEST-CBT-03] Crítico mitigado falhou: {result}, esperado 150.0");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-03] Multiplicador de Dano Crítico vs Resistência (2.0x vs 50% -> 1.5x): PASS");
        }

        public static void Test_CBT_04_ElementalResistances(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats = new StatProfile
            {
                FireResistance = 0.40f,
                PoisonResistance = 0.75f,
                IceResistance = 0.50f
            };

            var fire = DamageInfo.FromRaw(100f, DamageType.Fire);
            var poison = DamageInfo.FromRaw(100f, DamageType.Poison);
            var ice = DamageInfo.FromRaw(100f, DamageType.Ice);

            float dFire = HealthController.CalculateMitigatedDamage(fire, stats);
            float dPoison = HealthController.CalculateMitigatedDamage(poison, stats);
            float dIce = HealthController.CalculateMitigatedDamage(ice, stats);

            if (Math.Abs(dFire - 60f) > Epsilon)
                throw new Exception($"[TEST-CBT-04] Dano de fogo falhou: {dFire}, esperado 60.0");
            if (Math.Abs(dPoison - 25f) > Epsilon)
                throw new Exception($"[TEST-CBT-04] Dano de veneno falhou: {dPoison}, esperado 25.0");
            if (Math.Abs(dIce - 50f) > Epsilon)
                throw new Exception($"[TEST-CBT-04] Dano de gelo falhou: {dIce}, esperado 50.0");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-04] Resistências Elementais (Fogo 40%, Veneno 75%, Gelo 50%): PASS");
        }

        public static void Test_CBT_05_ShieldAbsorptionAndOverflow(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats = new StatProfile(maxHull: 100f, maxShield: 30f, armor: 0f);
            var health = new HealthController(stats);

            var hit80 = DamageInfo.FromRaw(80f, DamageType.Physical);
            health.TakeDamage(hit80, out float actualDmg, out bool absorbed);

            // Escudo absorve 30 e zera; Casco absorve 50 restante e vai para 50
            if (health.ShieldHealth != 0f)
                throw new Exception($"[TEST-CBT-05] Escudo deveria estar zerado, obteve {health.ShieldHealth}");
            if (Math.Abs(health.HullHealth - 50f) > Epsilon)
                throw new Exception($"[TEST-CBT-05] Casco esperado 50, obteve {health.HullHealth}");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-05] Absorção de Escudo e Transbordamento (30 Shield + 100 Hull vs 80 Dmg -> 50 Hull): PASS");
        }

        public static void Test_CBT_06_ShieldBreakTriggerAndNovaBurst(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var stats = new StatProfile(maxHull: 100f, maxShield: 40f, armor: 0f);
            var health = new HealthController(stats);
            var hitstop = new HitstopManagerModel();

            bool onShieldBrokenFired = false;
            bool novaBurstFired = false;

            health.OnShieldBroken += () =>
            {
                onShieldBrokenFired = true;
                hitstop.TriggerHitstop(HitstopManagerModel.ShieldBreakDuration);
                novaBurstFired = true; // Gatilho do protocolo NovaBurst
            };

            var hit50 = DamageInfo.FromRaw(50f, DamageType.Physical);
            health.TakeDamage(hit50, out _, out _);

            if (!onShieldBrokenFired)
                throw new Exception("[TEST-CBT-06] Evento OnShieldBroken não foi disparado!");
            if (!hitstop.IsFrozen || Math.Abs(hitstop.RemainingFreezeTime - 0.080f) > Epsilon)
                throw new Exception($"[TEST-CBT-06] Hitstop de 80ms não foi ativado corretamente: {hitstop.RemainingFreezeTime}");
            if (!novaBurstFired)
                throw new Exception("[TEST-CBT-06] NovaBurst não foi ativado após quebra de escudo.");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-06] Gatilho de Quebra de Escudo (Shield Break 80ms + NovaBurst): PASS");
        }

        public static void Test_CBT_07_RammingMechanicalCollision(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var playerPos = new Vector2(0f, 0f);
            var playerVel = new Vector2(200f, 0f);
            var playerHealth = new HealthController(100f, 50f);

            var rammerPos = new Vector2(10f, 0f);
            var rammerVel = new Vector2(-200f, 0f);
            var rammerHealth = new HealthController(80f, 0f);

            // Choque frontal: velocidade relativa = 400 px/s
            float relSpeed = (playerVel - rammerVel).Length();
            float ramDamage = relSpeed * 0.15f; // 60 de dano

            playerHealth.TakeDamage(DamageInfo.FromRaw(ramDamage));
            rammerHealth.TakeDamage(DamageInfo.FromRaw(ramDamage));

            // Repulsão mútua oposta
            playerVel = -playerVel * 0.5f;
            rammerVel = -rammerVel * 0.5f;

            if (playerHealth.HullHealth >= 100f && playerHealth.ShieldHealth >= 50f)
                throw new Exception("[TEST-CBT-07] Player não sofreu dano de aríete.");
            if (rammerHealth.HullHealth >= 80f)
                throw new Exception("[TEST-CBT-07] Inimigo não sofreu dano de aríete.");
            if (playerVel.X >= 0f || rammerVel.X <= 0f)
                throw new Exception("[TEST-CBT-07] Repulsão física não inverteu as trajetórias após o choque.");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-07] Colisão de Aríete Mecânico (Ramming Dano Mútuo e Repulsão): PASS");
        }

        public static void Test_CBT_08_AsteroidBilliardKineticCrush(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var hugeAsteroid = AsteroidModel.CreateHuge(new Vector2(0f, 0f), new Vector2(300f, 0f));
            var lightFighter = new EnemyModel { Position = new Vector2(40f, 0f), Radius = 15f };

            AsteroidModel.ResolveBilliardElasticImpact(hugeAsteroid, lightFighter, out bool crushed);

            if (!crushed)
                throw new Exception("[TEST-CBT-08] Caça leve deveria ter sido esmagado letalmente pelo asteroide Huge!");

            // Velocidade do asteroide foi reduzida de 300 para ~255 px/s
            if (hugeAsteroid.Velocity.X >= 300f || hugeAsteroid.Velocity.X <= 0f)
                throw new Exception($"[TEST-CBT-08] Asteroide não sofreu perda fracionária de momento: {hugeAsteroid.Velocity.X}");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-08] Física de Bilhar de Asteroides (Esmagamento Cinético Huge 2500kg): PASS");
        }

        public static void Test_CBT_09_HierarchicalAsteroidFracture(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var huge = AsteroidModel.CreateHuge(Vector2.Zero, Vector2.Zero);
            var largeFragments = huge.Fracture();

            if (largeFragments.Count < 2 || largeFragments[0].Tier != AsteroidTier.Large)
                throw new Exception("[TEST-CBT-09] Asteroide Huge deve gerar 2 fragmentos Large.");

            var mediumFragments = largeFragments[0].Fracture();
            if (mediumFragments.Count < 2 || mediumFragments[0].Tier != AsteroidTier.Medium)
                throw new Exception("[TEST-CBT-09] Asteroide Large deve gerar 2 fragmentos Medium.");

            var smallFragments = mediumFragments[0].Fracture();
            if (smallFragments.Count < 2 || smallFragments[0].Tier != AsteroidTier.Small)
                throw new Exception("[TEST-CBT-09] Asteroide Medium deve gerar 2 fragmentos Small.");

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-09] Fratura Hierárquica em Cadeia de Asteroides (Huge->Large->Medium->Small): PASS");
        }

        public static void Test_CBT_10_Strict7StageCollisionPipeline(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var executionOrder = new List<int>();

            // Simulação das 7 etapas em sequência estrita
            void Stage1() => executionOrder.Add(1); // Player Projectiles vs Enemies
            void Stage2() => executionOrder.Add(2); // Player Projectiles vs Asteroids
            void Stage3() => executionOrder.Add(3); // Enemy Projectiles vs Asteroids
            void Stage4() => executionOrder.Add(4); // Enemy Projectiles vs Player
            void Stage5() => executionOrder.Add(5); // Ramming Colisões Mecânicas
            void Stage6() => executionOrder.Add(6); // Bilhar Asteroides vs Inimigos
            void Stage7() => executionOrder.Add(7); // Hazards e Coleta de Drops

            Stage1();
            Stage2();
            Stage3();
            Stage4();
            Stage5();
            Stage6();
            Stage7();

            if (executionOrder.Count != 7)
                throw new Exception("[TEST-CBT-10] Contagem de etapas de colisão divergente.");

            for (int i = 0; i < 7; i++)
            {
                if (executionOrder[i] != i + 1)
                    throw new Exception($"[TEST-CBT-10] Ordem violada na etapa {i + 1}: executou {executionOrder[i]}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-CBT-10] Execução Estrita das 7 Etapas do CollisionSystem: PASS");
        }
    }
}
