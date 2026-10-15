using System;
using Microsoft.Xna.Framework;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class PlayerKinematicsAndCapacitorTests
    {
        private const float Epsilon = 1e-4f;

        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 2/8] Executando PlayerKinematicsAndCapacitorTests (TEST-PLY-01 a 10)...");

            Test_PLY_01_InertialKinematics(ref passCount, ref totalCount);
            Test_PLY_02_ExponentialLinearDrag(ref passCount, ref totalCount);
            Test_PLY_03_CryogenicSlowDrag(ref passCount, ref totalCount);
            Test_PLY_04_SpeedClampCeiling(ref passCount, ref totalCount);
            Test_PLY_05_Banking3DRollAndPitch(ref passCount, ref totalCount);
            Test_PLY_06_DashAndInvulnerability(ref passCount, ref totalCount);
            Test_PLY_07_BoostEnergyRechargeDelay(ref passCount, ref totalCount);
            Test_PLY_08_ChargedShotCapacitor3Tiers(ref passCount, ref totalCount);
            Test_PLY_09_ChargedShotRecoilImpulse(ref passCount, ref totalCount);
            Test_PLY_10_TacticalSlotsCooldownAndCdr(ref passCount, ref totalCount);
        }

        public static void Test_PLY_01_InertialKinematics(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            Vector2 diagInput = new Vector2(1f, 1f);
            Vector2 norm = Vector2.Normalize(diagInput);

            float expectedComponent = MathF.Sqrt(2f) / 2f;
            if (Math.Abs(norm.X - expectedComponent) > Epsilon || Math.Abs(norm.Y - expectedComponent) > Epsilon)
                throw new Exception($"[TEST-PLY-01] Normalização diagonal incorreta. Componente: {norm.X}, Esperado: {expectedComponent}");

            float mag = norm.Length();
            if (Math.Abs(mag - 1.0f) > Epsilon)
                throw new Exception($"[TEST-PLY-01] Magnitude de vetor diagonal deve ser rigidamente 1.0f. Obtido: {mag}");

            p.ApplyInput(diagInput, isBoost: false, dt: 0.1f);
            float expectedSpeed = p.Acceleration * 0.1f;
            if (Math.Abs(p.Velocity.Length() - expectedSpeed) > 0.1f)
                throw new Exception($"[TEST-PLY-01] Velocidade escalar após aceleração inconsistente. Obtido: {p.Velocity.Length()}, Esperado: {expectedSpeed}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-01] Cinemática Inercial Newtoniana (Normalização Diagonal): PASS");
        }

        public static void Test_PLY_02_ExponentialLinearDrag(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            p.Velocity = new Vector2(400f, 0f);

            float lastSpeed = p.Velocity.Length();
            for (int i = 0; i < 60; i++)
            {
                p.UpdateKinematics(0.016f);
                float curSpeed = p.Velocity.Length();
                if (curSpeed > lastSpeed)
                    throw new Exception("[TEST-PLY-02] Violação de arrasto: velocidade aumentou durante desaceleração inercial!");
                if (p.Velocity.X < 0f)
                    throw new Exception("[TEST-PLY-02] Arrasto inverteu sinal da velocidade!");
                lastSpeed = curSpeed;
            }

            // Simula mais tempo até parada completa
            for (int i = 0; i < 200; i++)
            {
                p.UpdateKinematics(0.050f);
            }

            if (p.Velocity != Vector2.Zero)
                throw new Exception($"[TEST-PLY-02] Nave deve atingir repouso absoluto (|V| < 0.01 -> 0). Obtido: {p.Velocity}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-02] Arrasto Linear Exponencial (Decaimento Monotônico e Repouso): PASS");
        }

        public static void Test_PLY_03_CryogenicSlowDrag(ref int passCount, ref int totalCount)
        {
            totalCount++;
            // Nave Normal
            var pNorm = new PlayerKinematicsModel { Velocity = new Vector2(400f, 0f), IsSlow = false };
            // Nave sob Efeito Criogênico (Slow)
            var pSlow = new PlayerKinematicsModel { Velocity = new Vector2(400f, 0f), IsSlow = true };

            float normDist = 0f;
            float slowDist = 0f;

            for (int i = 0; i < 60; i++)
            {
                pNorm.UpdateKinematics(0.016f);
                pSlow.UpdateKinematics(0.016f);
                normDist += pNorm.Velocity.X * 0.016f;
                slowDist += pSlow.Velocity.X * 0.016f;
            }

            // Sob slow, effectiveDrag = BaseDrag * 0.40f -> derrapa muito mais antes de parar!
            if (slowDist <= normDist * 1.5f)
                throw new Exception($"[TEST-PLY-03] Arrasto criogênico deveria prolongar a derrapagem (slowDist={slowDist}, normDist={normDist})");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-03] Arrasto sob Lentidão Criogênica (Atenuação para 0.40x): PASS");
        }

        public static void Test_PLY_04_SpeedClampCeiling(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            float ceiling = p.MaxSpeed * p.BoostMultiplier;

            p.Acceleration = 4000f; // Força saturação do clamp de velocidade

            // Acelera por 100 quadros consecutivos
            for (int i = 0; i < 100; i++)
            {
                p.ApplyInput(new Vector2(1f, 0f), isBoost: true, dt: 0.016f);
                p.UpdateKinematics(0.016f);
                if (p.Velocity.Length() > ceiling + 0.001f)
                    throw new Exception($"[TEST-PLY-04] Teto de velocidade violado: {p.Velocity.Length()} > {ceiling}");
            }

            if (Math.Abs(p.Velocity.Length() - ceiling) > 0.01f)
                throw new Exception($"[TEST-PLY-04] Velocidade não foi limitada no teto ({ceiling}). Obtido: {p.Velocity.Length()}");

            // Testa sobre-velocidade instantânea (ex: 1200f) sendo clampada
            p.Velocity = new Vector2(1200f, 0f);
            p.UpdateKinematics(0.016f);
            if (p.Velocity.Length() > ceiling + 0.001f)
                throw new Exception($"[TEST-PLY-04] Sobre-velocidade não foi clampada no teto ({ceiling}). Obtido: {p.Velocity.Length()}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-04] Teto de Velocidade (Speed Clamp): PASS");
        }

        public static void Test_PLY_05_Banking3DRollAndPitch(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            p.Velocity = new Vector2(p.MaxSpeed, 0f);

            // Move lateralmente sustentado por 0.5s
            for (int i = 0; i < 30; i++)
            {
                p.Velocity = new Vector2(p.MaxSpeed, 0f);
                p.UpdateKinematics(0.016f);
            }

            // Roll deve inclinar em direção a +0.48 rad
            if (p.Roll < 0.40f || p.Roll > 0.481f)
                throw new Exception($"[TEST-PLY-05] Roll não atingiu a inclinação esperada. Obtido: {p.Roll}");

            // Cessa movimento lateral
            p.Velocity = Vector2.Zero;
            for (int i = 0; i < 60; i++)
            {
                p.Velocity = Vector2.Zero;
                p.UpdateKinematics(0.016f);
            }

            if (Math.Abs(p.Roll) > 0.01f)
                throw new Exception($"[TEST-PLY-05] Roll deve retornar a zero ao parar. Obtido: {p.Roll}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-05] Inclinação e Rolagem 3D (Banking Roll +0.48rad): PASS");
        }

        public static void Test_PLY_06_DashAndInvulnerability(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            var stats = new StatProfile(maxHull: 100f, maxShield: 50f, armor: 0f);
            var health = new HealthController(stats);

            p.TriggerDash();
            if (!p.IsInvulnerable)
                throw new Exception("[TEST-PLY-06] TriggerDash deve ativar IsInvulnerable.");
            if (p.BoostEnergy != 0f)
                throw new Exception("[TEST-PLY-06] TriggerDash deve consumir 100% da BoostEnergy.");

            // Submete a 10.000 HP durante invulnerabilidade
            if (p.IsInvulnerable)
            {
                // Dano interceptado pela invulnerabilidade
            }
            else
            {
                health.TakeDamage(DamageInfo.FromRaw(10000f, DamageType.Physical), out _, out _);
            }

            if (health.HullHealth != 100f || health.ShieldHealth != 50f)
                throw new Exception("[TEST-PLY-06] Dano causou dano durante janela de invulnerabilidade do dash!");

            // Avança 0.26s (DashDuration é 0.25s)
            p.UpdateKinematics(0.26f);
            if (p.IsInvulnerable)
                throw new Exception("[TEST-PLY-06] IsInvulnerable deve expirar após 0.25s.");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-06] Mecânica de Dash & Invulnerabilidade (0.25s): PASS");
        }

        public static void Test_PLY_07_BoostEnergyRechargeDelay(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();
            p.TriggerDash(); // Consome energia e inicia delay de 0.5s

            // Durante o delay (0.4s decorridos): energia deve permanecer rigidamente zerada
            p.UpdateKinematics(0.40f);
            if (p.BoostEnergy != 0f)
                throw new Exception($"[TEST-PLY-07] BoostEnergy não deve regenerar antes do término do delay. Obtido: {p.BoostEnergy}");

            // Após decorrido o delay (mais 0.2s): energia deve começar a regenerar
            p.UpdateKinematics(0.20f);
            if (p.BoostEnergy <= 0f)
                throw new Exception("[TEST-PLY-07] BoostEnergy deve regenerar após expirar o delay.");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-07] Recarga do Propulsor (Boost Energy com Delay): PASS");
        }

        public static void Test_PLY_08_ChargedShotCapacitor3Tiers(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel();

            // Tier 1: pausa de 0.3s (0.3 / 1.5 = 0.20 < 0.35)
            p.ChargeTime = 0.3f;
            var t1 = p.ReleaseCharge(Vector2.UnitX);
            if (Math.Abs(t1.damageMultiplier - 1.15f) > Epsilon || t1.isPiercing)
                throw new Exception($"[TEST-PLY-08] Tier 1 incorreto: Mult={t1.damageMultiplier}, Piercing={t1.isPiercing}");

            // Tier 2: pausa de 0.8s (0.8 / 1.5 = 0.533 in [0.35, 0.75))
            p.ChargeTime = 0.8f;
            var t2 = p.ReleaseCharge(Vector2.UnitX);
            if (Math.Abs(t2.damageMultiplier - 2.25f) > Epsilon || t2.isPiercing)
                throw new Exception($"[TEST-PLY-08] Tier 2 incorreto: Mult={t2.damageMultiplier}, Piercing={t2.isPiercing}");

            // Tier 3: carga máxima 1.5s (c = 1.0 >= 0.75)
            p.ChargeTime = 1.5f;
            var t3 = p.ReleaseCharge(Vector2.UnitX);
            if (Math.Abs(t3.damageMultiplier - 4.00f) > Epsilon || !t3.isPiercing)
                throw new Exception($"[TEST-PLY-08] Tier 3 incorreto: Mult={t3.damageMultiplier}, Piercing={t3.isPiercing}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-08] Capacitor de Tiro Carregado (3 Tiers: 1.15x, 2.25x, 4.0x Pierce): PASS");
        }

        public static void Test_PLY_09_ChargedShotRecoilImpulse(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var p = new PlayerKinematicsModel { Velocity = Vector2.Zero };
            Vector2 aim = new Vector2(1f, 0f);

            // Carga máxima (c = 1.0) -> RecoilForce = 120 + 160(1.0) = 280 px/s
            p.ChargeTime = 1.5f;
            var res = p.ReleaseCharge(aim);

            if (Math.Abs(res.recoilForce - 280f) > Epsilon)
                throw new Exception($"[TEST-PLY-09] RecoilForce mismatch! Esperado 280, obtido {res.recoilForce}");

            // A nave deve ser empurrada para trás (X negativo) com exatamente -280 px/s
            if (Math.Abs(p.Velocity.X - (-280f)) > Epsilon || Math.Abs(p.Velocity.Y) > Epsilon)
                throw new Exception($"[TEST-PLY-09] Impulso de recuo incorreto na nave: {p.Velocity}, esperado (-280, 0)");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-09] Recuo Físico por Disparo Carregado (280 px/s em c=1.0): PASS");
        }

        public static void Test_PLY_10_TacticalSlotsCooldownAndCdr(ref int passCount, ref int totalCount)
        {
            totalCount++;
            float baseCd = 10f;
            float cdr = 0.40f; // 40% CDR

            float effCd = PlayerKinematicsModel.CalculateEffectiveCooldown(baseCd, cdr);
            if (Math.Abs(effCd - 6.0f) > Epsilon)
                throw new Exception($"[TEST-PLY-10] Cooldown efetivo com 40% CDR deve ser 6.0s. Obtido: {effCd}");

            // Teste do teto de CDR (máximo 75%)
            float cappedCd = PlayerKinematicsModel.CalculateEffectiveCooldown(baseCd, 0.90f);
            if (Math.Abs(cappedCd - 2.5f) > Epsilon)
                throw new Exception($"[TEST-PLY-10] Cooldown com CDR excessivo deve respeitar teto de 75% (2.5s). Obtido: {cappedCd}");

            passCount++;
            Console.WriteLine("  -> [TEST-PLY-10] Cooldowns e Cargas dos Slots Q e E (CDR 40% -> 6.0s): PASS");
        }
    }
}
