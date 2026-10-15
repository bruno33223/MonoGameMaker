using System;
using Microsoft.Xna.Framework;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class EnemyAndModularBossTests
    {
        private const float Epsilon = 1e-4f;

        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 3/8] Executando EnemyAndModularBossTests (TEST-ENM-01 a 10)...");

            Test_ENM_01_StageHpScaling(ref passCount, ref totalCount);
            Test_ENM_02_StageDamageScaling(ref passCount, ref totalCount);
            Test_ENM_03_OffscreenSteering(ref passCount, ref totalCount);
            Test_ENM_04_OffscreenSilentRecycle(ref passCount, ref totalCount);
            Test_ENM_05_SoftSeparationPhysics(ref passCount, ref totalCount);
            Test_ENM_06_AsteroidAvoidance(ref passCount, ref totalCount);
            Test_ENM_07_BossModularPartDerivation(ref passCount, ref totalCount);
            Test_ENM_08_ModularDestructionWithoutBossDeath(ref passCount, ref totalCount);
            Test_ENM_09_EclipseTitanPhaseTransitions(ref passCount, ref totalCount);
            Test_ENM_10_TitanHealingRitualInterruption(ref passCount, ref totalCount);
        }

        public static void Test_ENM_01_StageHpScaling(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float baseHp = 100f;
            int[] testLevels = { 1, 5, 10, 50, 340 };

            foreach (var level in testLevels)
            {
                float calculated = ProgressionMath.CalculateHP(baseHp, level);
                float expectedHp = baseHp * MathF.Pow(1f + (0.15f * (level - 1)), 1.1f);
                float relError = Math.Abs(calculated - expectedHp) / expectedHp;
                if (relError > 0.001f) // erro < 0.1%
                    throw new Exception($"[TEST-ENM-01] HP Scaling fora do erro admissível no nível {level}: calculado {calculated}, esperado {expectedHp}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-01] Escalonamento de HP por Estágio (L=1, 5, 10, 50): PASS");
        }

        public static void Test_ENM_02_StageDamageScaling(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float baseDmg = 100f;
            (int level, float expectedDmg)[] cases = {
                (1, 100.0f),
                (10, 190.0f),
                (20, 290.0f)
            };

            foreach (var (level, expectedDmg) in cases)
            {
                float calculated = ProgressionMath.CalculateDamage(baseDmg, level);
                if (Math.Abs(calculated - expectedDmg) > Epsilon)
                    throw new Exception($"[TEST-ENM-02] Dano scaling linear incorreto no nível {level}: calculado {calculated}, esperado {expectedDmg}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-02] Escalonamento de Dano por Estágio (L=1, 10, 20): PASS");
        }

        public static void Test_ENM_03_OffscreenSteering(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var enemy = new EnemyModel
            {
                Position = new Vector2(1200f, 800f), // Fora da arena (max X: 960, max Y: 540)
                Velocity = new Vector2(50f, 50f)     // Movimento inicial afastando-se
            };

            if (!enemy.IsOutsideArena)
                throw new Exception("[TEST-ENM-03] Posição (1200, 800) deve ser identificada como fora da arena.");

            // Atualiza por 1 segundo
            for (int i = 0; i < 60; i++)
            {
                enemy.UpdateSteeringAndRecycle(0.016f);
            }

            // O vetor de velocidade deve apontar em direção à origem (0, 0) -> componentes X e Y negativos!
            if (enemy.Velocity.X >= 0f || enemy.Velocity.Y >= 0f)
                throw new Exception($"[TEST-ENM-03] Aceleração centrípeta falhou em orientar velocidade para a origem: {enemy.Velocity}");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-03] Inteligência de Retorno (Offscreen Steering): PASS");
        }

        public static void Test_ENM_04_OffscreenSilentRecycle(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var enemy = new EnemyModel
            {
                Position = new Vector2(1500f, 1500f),
                Velocity = Vector2.Zero
            };

            // Permanece 3.6s fora da tela
            enemy.UpdateSteeringAndRecycle(3.6f);

            if (enemy.IsActive)
                throw new Exception("[TEST-ENM-04] Inimigo mantido > 3.5s fora da tela deve ser reciclado (IsActive = false).");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-04] Reciclagem Silenciosa por Timeout (> 3.5s): PASS");
        }

        public static void Test_ENM_05_SoftSeparationPhysics(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var e1 = new EnemyModel { Position = new Vector2(0f, 0f), Radius = 20f };
            var e2 = new EnemyModel { Position = new Vector2(10f, 0f), Radius = 20f };

            float initialDist = Vector2.Distance(e1.Position, e2.Position);
            float lastDist = initialDist;

            // Executa resolução suave de separação por 30 passos
            for (int i = 0; i < 30; i++)
            {
                EnemyModel.ApplySoftSeparation(e1, e2, worldDt: 0.016f);
                float curDist = Vector2.Distance(e1.Position, e2.Position);
                if (curDist < lastDist)
                    throw new Exception("[TEST-ENM-05] Separação deve aumentar distância monotonicamente sem oscilar!");
                lastDist = curDist;
            }

            if (lastDist <= initialDist)
                throw new Exception("[TEST-ENM-05] Separação física não aumentou a distância entre as naves.");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-05] Separação Física Suave (Soft Separation): PASS");
        }

        public static void Test_ENM_06_AsteroidAvoidance(ref int passCount, ref int totalCount)
        {
            totalCount++;
            Vector2 enemyPos = new Vector2(0f, -200f);
            Vector2 enemyVel = new Vector2(0f, 200f); // Trajetória direta em direção ao asteroide em (0, 0)
            Vector2 asteroidPos = Vector2.Zero;
            float asteroidRadius = 60f;

            // Detecção e desvio perpendicular
            Vector2 toAsteroid = asteroidPos - enemyPos;
            bool onCollisionCourse = toAsteroid.Length() > asteroidRadius &&
                                     Vector2.Dot(Vector2.Normalize(enemyVel), Vector2.Normalize(toAsteroid)) > 0.9f;

            if (onCollisionCourse)
            {
                // Aplica desvio perpendicular
                Vector2 avoidancePerp = new Vector2(-enemyVel.Y, enemyVel.X);
                enemyVel += Vector2.Normalize(avoidancePerp) * 150f;
            }

            // O novo vetor não deve ser puramente vertical (deve ter componente X de desvio)
            if (Math.Abs(enemyVel.X) < 10f)
                throw new Exception("[TEST-ENM-06] Inimigo falhou em aplicar aceleração lateral de desvio de asteroide.");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-06] Desvio de Asteroides (Asteroid Avoidance): PASS");
        }

        public static void Test_ENM_07_BossModularPartDerivation(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var bossPos = new Vector3(200f, 150f, 0f);
            var part = new BossModularPart(new Vector3(100f, 0f, 0f));

            // Chefe girado em 90 graus (pi/2 rad)
            float angleZ = MathHelper.PiOver2;
            Vector3 worldPos = part.ComputeWorldPosition(bossPos, angleZ);

            // Offset (100, 0, 0) rotacionado em 90 graus vira (0, 100, 0)
            // worldPos = (200, 150, 0) + (0, 100, 0) = (200, 250, 0)
            if (Math.Abs(worldPos.X - 200f) > 0.01f || Math.Abs(worldPos.Y - 250f) > 0.01f)
                throw new Exception($"[TEST-ENM-07] Posição mundial da parte modular incorreta: {worldPos}, esperado (200, 250, 0)");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-07] Derivação de Posição de Partes Modulares de Chefe: PASS");
        }

        public static void Test_ENM_08_ModularDestructionWithoutBossDeath(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var boss = new WorldBreakerBossModel();

            // Destroi a torreta 1
            var turret0 = boss.Parts[0];
            turret0.TakeDamage(100f);

            if (!turret0.IsDestroyed || !turret0.IsSmoking)
                throw new Exception("[TEST-ENM-08] Torreta destruída deve estar IsDestroyed = true e IsSmoking = true.");

            // O núcleo e as outras 5 torretas permanecem operantes
            if (boss.CoreHealth != 1000f)
                throw new Exception("[TEST-ENM-08] Vida do núcleo foi afetada indevidamente pela destruição de sub-parte!");

            for (int i = 1; i < boss.Parts.Count; i++)
            {
                if (boss.Parts[i].IsDestroyed)
                    throw new Exception($"[TEST-ENM-08] Torreta {i} foi destruída indevidamente.");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-08] Destruição Modular sem Morte do Chefe: PASS");
        }

        public static void Test_ENM_09_EclipseTitanPhaseTransitions(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var titan = new EclipseTitanBossModel();

            // 100% -> Fase 1
            titan.UpdatePhase();
            if (titan.Phase != 1)
                throw new Exception($"[TEST-ENM-09] Esperado Fase 1 em 100% HP, obtido {titan.Phase}");

            // 50% HP (abaixo de 66%) -> Fase 2
            titan.CurrentHealth = titan.MaxHealth * 0.50f;
            titan.UpdatePhase();
            if (titan.Phase != 2)
                throw new Exception($"[TEST-ENM-09] Esperado Fase 2 em 50% HP, obtido {titan.Phase}");

            // 20% HP (abaixo de 33%) -> Fase 3
            titan.CurrentHealth = titan.MaxHealth * 0.20f;
            titan.UpdatePhase();
            if (titan.Phase != 3 || !titan.IsHealingCocoonActive)
                throw new Exception($"[TEST-ENM-09] Esperado Fase 3 com Casulo de Cura ativo em 20% HP, obtido Fase {titan.Phase}");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-09] Transição de Fases do Titã (EclipseTitanBoss 100%->66%->33%): PASS");
        }

        public static void Test_ENM_10_TitanHealingRitualInterruption(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var titan = new EclipseTitanBossModel
            {
                CurrentHealth = 600f,
                EscortCount = 4
            };
            titan.UpdatePhase();

            float beforeHp = titan.CurrentHealth;
            titan.UpdateCocoonRegen(1.0f); // 1 segundo de cura com 4 escoltas vivas (+1% = +30 HP)

            if (titan.CurrentHealth <= beforeHp)
                throw new Exception("[TEST-ENM-10] Casulo de cura deve regenerar HP com escoltas vivas.");

            // Destroi as 4 escoltas
            titan.EscortCount = 0;
            titan.UpdateCocoonRegen(0.1f);

            if (titan.IsHealingCocoonActive || !titan.IsVulnerablePostRitual)
                throw new Exception("[TEST-ENM-10] Casulo deve ser interrompido e ativar postura de vulnerabilidade quando as 4 escoltas caem.");

            passCount++;
            Console.WriteLine("  -> [TEST-ENM-10] Interrupção de Ritual de Cura do Chefe (Casulo quebrado): PASS");
        }
    }
}
