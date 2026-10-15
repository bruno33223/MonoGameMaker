using System;

namespace TestConsole.Tests.Domain
{
    /// <summary>
    /// Modelo de simulação determinística de Hitstop e Impact Flash.
    /// </summary>
    public class HitstopManagerModel
    {
        public const float HeavyHitDuration = 0.035f;
        public const float CriticalHitDuration = 0.055f;
        public const float ShieldBreakDuration = 0.080f;
        public const float BossDeathDuration = 0.120f;

        private float _freezeTimer;
        private float _impactFlashIntensity;

        public bool IsFrozen => _freezeTimer > 0f;
        public float RemainingFreezeTime => _freezeTimer;
        public float FreezeFactor => IsFrozen ? 0.0f : 1.0f;
        public float ImpactFlashIntensity => _impactFlashIntensity;

        public void TriggerHitstop(float durationSeconds, float impactFlash = 0f)
        {
            if (durationSeconds <= 0f) return;

            // Não-aditivo destrutivo, mas extensivo: assume o maior tempo
            if (durationSeconds > _freezeTimer)
            {
                _freezeTimer = durationSeconds;
            }

            if (impactFlash > _impactFlashIntensity)
            {
                _impactFlashIntensity = Math.Clamp(impactFlash, 0f, 1.0f);
            }
        }

        public void TriggerImpactFlash(float intensity = 0.80f)
        {
            _impactFlashIntensity = Math.Clamp(intensity, 0f, 1.0f);
        }

        public void Update(float dt, float flashDecayRate = 2.5f)
        {
            if (dt <= 0f) return;

            if (_freezeTimer > 0f)
            {
                _freezeTimer -= dt;
                if (_freezeTimer <= 0f)
                {
                    _freezeTimer = 0f;
                }
            }

            if (_impactFlashIntensity > 0f)
            {
                _impactFlashIntensity = MathF.Max(0f, _impactFlashIntensity - (flashDecayRate * dt));
            }
        }

        public void Reset()
        {
            _freezeTimer = 0f;
            _impactFlashIntensity = 0f;
        }
    }
}
