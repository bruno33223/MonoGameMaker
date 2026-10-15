using System;
using Microsoft.Xna.Framework;

namespace TestConsole.Tests.Domain
{
    public class PlayerKinematicsModel
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        public float MaxSpeed { get; set; } = 400f;
        public float Acceleration { get; set; } = 1200f;
        public float BaseDrag { get; set; } = 3.0f;
        public float BoostMultiplier { get; set; } = 1.5f;

        public float Roll { get; set; } = 0f;
        public float Pitch { get; set; } = 0f;

        public bool IsSlow { get; set; } = false;
        public bool IsInvulnerable { get; set; } = false;
        public float DashTimer { get; set; } = 0f;
        public const float DashDuration = 0.25f;

        public float BoostEnergy { get; set; } = 1.0f;
        public float BoostEnergyRechargeDelay { get; set; } = 0.5f;
        private float _boostDelayTimer = 0f;

        public float ChargeTime { get; set; } = 0f;
        public const float MaxChargeTime = 1.5f;

        public float QCooldownTimer { get; set; } = 0f;
        public float ECooldownTimer { get; set; } = 0f;

        private Vector2 _lastInputDelta = Vector2.Zero;

        public void ApplyInput(Vector2 input, bool isBoost, float dt)
        {
            if (input.LengthSquared() > 0.0001f)
            {
                Vector2 dirNorm = Vector2.Normalize(input);
                float boostMult = isBoost ? BoostMultiplier : 1.0f;
                _lastInputDelta = dirNorm * (Acceleration * boostMult) * dt;
                Velocity += _lastInputDelta;
            }
            else
            {
                _lastInputDelta = Vector2.Zero;
            }
        }

        public void UpdateKinematics(float dt)
        {
            // Arrasto aplicado sobre o estado anterior ao input para convergência assintótica newtoniana exata (v = a / drag)
            float effectiveDrag = BaseDrag * (IsSlow ? 0.40f : 1.0f);
            Vector2 preInputVelocity = Velocity - _lastInputDelta;
            preInputVelocity -= preInputVelocity * effectiveDrag * dt;
            Velocity = preInputVelocity + _lastInputDelta;
            _lastInputDelta = Vector2.Zero;

            if (Velocity.Length() < 0.01f)
            {
                Velocity = Vector2.Zero;
            }

            // Speed clamp
            float currentMax = MaxSpeed * (BoostEnergy > 0f ? BoostMultiplier : 1.0f);
            if (Velocity.Length() > currentMax)
            {
                Velocity = Vector2.Normalize(Velocity) * currentMax;
            }

            Position += Velocity * dt;

            // Banking 3D
            Roll = MathHelper.Lerp(Roll, Math.Clamp(Velocity.X / MaxSpeed, -1f, 1f) * 0.48f, 10.0f * dt);
            Pitch = MathHelper.Lerp(Pitch, Math.Clamp(Velocity.Y / MaxSpeed, -1f, 1f) * 0.22f, 8.0f * dt);

            // Dash & Invulnerabilidade
            if (DashTimer > 0f)
            {
                DashTimer -= dt;
                if (DashTimer <= 0f)
                {
                    DashTimer = 0f;
                    IsInvulnerable = false;
                }
            }

            // Recarga de BoostEnergy após delay
            if (_boostDelayTimer > 0f)
            {
                float timeInDelay = Math.Min(_boostDelayTimer, dt);
                _boostDelayTimer -= timeInDelay;
                float timeAfterDelay = dt - timeInDelay;
                if (timeAfterDelay > 0f && BoostEnergy < 1.0f)
                {
                    BoostEnergy = Math.Min(1.0f, BoostEnergy + (0.5f * timeAfterDelay));
                }
            }
            else if (BoostEnergy < 1.0f)
            {
                BoostEnergy = Math.Min(1.0f, BoostEnergy + (0.5f * dt));
            }
        }

        public void TriggerDash()
        {
            DashTimer = DashDuration;
            IsInvulnerable = true;
            BoostEnergy = 0f;
            _boostDelayTimer = BoostEnergyRechargeDelay;
        }

        public void AccumulateCharge(float dt)
        {
            ChargeTime = Math.Min(MaxChargeTime, ChargeTime + dt);
        }

        public (float damageMultiplier, bool isPiercing, float recoilForce) ReleaseCharge(Vector2 aimDirection)
        {
            float c = Math.Clamp(ChargeTime / MaxChargeTime, 0f, 1f);
            float dmgMult;
            bool piercing;

            if (c < 0.35f)
            {
                dmgMult = 1.15f;
                piercing = false;
            }
            else if (c < 0.75f)
            {
                dmgMult = 2.25f;
                piercing = false;
            }
            else
            {
                dmgMult = 4.00f;
                piercing = true;
            }

            float recoil = 120f + (160f * c);
            if (aimDirection.LengthSquared() > 0.0001f)
            {
                Velocity -= Vector2.Normalize(aimDirection) * recoil;
            }

            ChargeTime = 0f;
            return (dmgMult, piercing, recoil);
        }

        public static float CalculateEffectiveCooldown(float baseCooldown, float cdr)
        {
            return baseCooldown * (1.0f - Math.Clamp(cdr, 0f, 0.75f));
        }
    }
}
