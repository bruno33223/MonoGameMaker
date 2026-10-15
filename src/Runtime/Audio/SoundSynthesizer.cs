using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MonoGameMaker.Runtime.Audio
{
    /// <summary>
    /// Sintetizador procedural de áudio DSP HD para estética sci-fi cinematográfica abafada (estilo Nova Drift),
    /// combinando dinâmicas multi-estágio em explosões (crack estridente + corpo + sub-bass), acúmulo de detonações
    /// e síntese holográfica cristalina de alta frequência para interface/menus.
    /// </summary>
    public class SoundSynthesizer : IDisposable
    {
        public const int SampleRate = 44100;
        private readonly Dictionary<SoundType, SoundEffect> _effectCache = new();

        public SoundSynthesizer()
        {
            PrecomputeAllSounds();
        }

        private void PrecomputeAllSounds()
        {
            foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
            {
                // Evita recalcular aliases redundantes que apontam para o mesmo valor numérico
                if (_effectCache.ContainsKey(type)) continue;

                byte[] pcm = GenerateRawPCM(type);
                try
                {
                    var effect = new SoundEffect(pcm, SampleRate, AudioChannels.Mono);
                    _effectCache[type] = effect;
                }
                catch
                {
                    // Fallback gracioso para ambientes de teste sem hardware de áudio
                }
            }

            // Garante que o AudioManager conheça o cache
            AudioManager.Instance.InitializeCache(_effectCache);
        }

        public SoundEffect? GetEffect(SoundType type)
        {
            _effectCache.TryGetValue(type, out var effect);
            return effect;
        }

        public void Play(SoundType type, float volume = 1f, float pitch = 0f)
        {
            AudioManager.Instance.Play(type, volume, pitch);
        }

        public void PlayExplosion(ExplosionScale scale = ExplosionScale.Medium, float volume = 1f)
        {
            AudioManager.Instance.PlayExplosion(scale, volume);
        }

        public void PlayLoop(SoundType type, object sourceKey, float volume = 1f)
        {
            AudioManager.Instance.PlayLoop(type, sourceKey, volume);
        }

        public void StopLoop(object sourceKey)
        {
            AudioManager.Instance.StopLoop(sourceKey);
        }

        public void StopAllLoops()
        {
            AudioManager.Instance.StopAllLoops();
        }

        public void Update(float dt, double totalGameTimeSeconds = 0)
        {
            AudioManager.Instance.Update(dt, totalGameTimeSeconds);
        }

        public void Dispose()
        {
            AudioManager.Instance.StopAllLoops();
            foreach (var effect in _effectCache.Values)
            {
                if (!effect.IsDisposed) effect.Dispose();
            }
            _effectCache.Clear();
        }

        #region Métodos de Interface e Conveniência
        public void PlayCriticalHit(float volume = 0.85f, float pitch = 0f) => Play(SoundType.CriticalHitPunch, volume, pitch);
        public void PlayUiHover(float volume = 0.40f) => Play(SoundType.UiHover, volume);
        public void PlayUiSelect(float volume = 0.60f) => Play(SoundType.UiSelect, volume);
        public void PlayUiBack(float volume = 0.50f) => Play(SoundType.UiBack, volume);
        public void PlayUiUpgradePurchased(float volume = 0.70f) => Play(SoundType.UiUpgradePurchased, volume);
        public void PlayPlayerChargeUp(float volume = 0.75f, float pitch = 0f) => Play(SoundType.PlayerChargeUp, volume, pitch);
        public void PlayPlayerChargeRelease(float volume = 0.85f, float pitch = 0f) => Play(SoundType.PlayerChargeRelease, volume, pitch);
        #endregion

        #region Métodos de Compatibilidade Retroativa Delegados
        public void PlayLaser(float pitch = 0f, float volume = 0.5f) => Play(SoundType.PlayerShotLight, volume, pitch);
        public void PlayPlasma(float pitch = 0f, float volume = 0.6f) => Play(SoundType.PlayerShotHeavy, volume, pitch);
        public void PlayAlienLaser(float pitch = 0f, float volume = 0.5f) => Play(SoundType.EnemyShotPlasma, volume, pitch);
        public void PlayExplosion(float pitch = 1f, float volume = 0.8f)
        {
            ExplosionScale scale = pitch switch
            {
                > 1.25f => ExplosionScale.Small,
                < 0.75f => ExplosionScale.Heavy,
                _ => ExplosionScale.Medium
            };
            AudioManager.Instance.PlayExplosion(scale, volume);
        }
        public void PlayPowerUp(float volume = 0.75f) => Play(SoundType.PowerupCollect, volume, 0f);
        public void PlayShieldHit(float volume = 0.6f) => Play(SoundType.ImpactShield, volume, 0f);
        public void PlayRockHit(float volume = 0.7f) => Play(SoundType.RockHit, volume, 0f);
        public void PlayWaveAlert(float volume = 0.8f) => Play(SoundType.WaveAlert, volume, 0f);
        public void PlayDash(float volume = 0.7f) => Play(SoundType.DashWhoosh, volume, 0f);
        public void PlaySpecial(float volume = 0.9f) => Play(SoundType.SpecialNova, volume, 0f);
        public void PlayEmpBlast(float volume = 0.85f) => Play(SoundType.EmpBlast, volume, 0f);
        public void PlayCryoBomb(float volume = 0.85f) => Play(SoundType.KyrosCrystalResonance, volume, 0f);
        public void PlayTachyonGlitch(float volume = 0.80f) => Play(SoundType.ChronosTachyonGlitch, volume, 0f);
        public void PlayWarpCollapse(float volume = 0.85f) => Play(SoundType.ChronosWarpCollapse, volume, 0f);
        public void PlayTitanBass(float volume = 0.90f) => Play(SoundType.TitanIndustrialBass, volume, 0f);
        public void PlayMissileLaunch(float volume = 0.75f) => Play(SoundType.EnemyShotHeavy, volume, 0f);
        public void PlayBossBeam(float volume = 0.95f) => Play(SoundType.BossEnergyBlast, volume, 0f);
        public void PlayBossChargeUp(float volume = 0.85f, float pitch = 0f) => Play(SoundType.BossChargeUp, volume, pitch);
        public void PlayBossTelegraph(float volume = 0.80f, float pitch = 0f) => Play(SoundType.BossAttackTelegraph, volume, pitch);
        public void PlayStatusSlowApplied(float volume = 0.70f, float pitch = 0f) => Play(SoundType.StatusSlowApplied, volume, pitch);
        public void PlayStatusPoisonApplied(float volume = 0.70f, float pitch = 0f) => Play(SoundType.StatusPoisonApplied, volume, pitch);
        public void PlayAcidSizzle(float volume = 0.6f) => Play(SoundType.AcidSizzle, volume, 0f);
        public void PlayPlayerDeath() => PlayExplosion(ExplosionScale.Heavy, 1.0f);

        // Faction SFX Dedicated Conveniences
        public void PlayToxisSpit(float volume = 0.60f, float pitch = 0f) => Play(SoundType.ToxisSpitBioAcid, volume, pitch);
        public void PlayToxisCharge(float volume = 0.80f, float pitch = 0f) => Play(SoundType.ToxisChargeWarn, volume, pitch);
        public void PlayToxisExplosion(float volume = 0.90f, float pitch = 0f) => Play(SoundType.ToxisDeathExplosion, volume, pitch);

        public void PlayKyrosShot(float volume = 0.60f, float pitch = 0f) => Play(SoundType.KyrosCrystalResonance, volume, pitch);
        public void PlayKyrosCharge(float volume = 0.80f, float pitch = 0f) => Play(SoundType.KyrosChargeWarn, volume, pitch);
        public void PlayKyrosShatter(float volume = 0.90f, float pitch = 0f) => Play(SoundType.KyrosShatter, volume, pitch);

        public void PlayTitanBass(float volume = 0.70f, float pitch = 0f) => Play(SoundType.TitanIndustrialBass, volume, pitch);
        public void PlayTitanCharge(float volume = 0.85f, float pitch = 0f) => Play(SoundType.TitanChargeWarn, volume, pitch);
        public void PlayTitanDestruction(float volume = 0.95f, float pitch = 0f) => Play(SoundType.TitanDestructionSub, volume, pitch);

        public void PlayChronosGlitch(float volume = 0.60f, float pitch = 0f) => Play(SoundType.ChronosTachyonGlitch, volume, pitch);
        public void PlayChronosPhaseWarn(float volume = 0.85f, float pitch = 0f) => Play(SoundType.ChronosPhaseWarn, volume, pitch);
        public void PlayChronosCollapse(float volume = 0.90f, float pitch = 0f) => Play(SoundType.ChronosWarpCollapse, volume, pitch);
        #endregion

        #region Pipeline DSP e Primitivas de Síntese

        /// <summary>
        /// Gera o buffer PCM mono 16-bit 44.1kHz para qualquer SoundType especificado.
        /// </summary>
        public static byte[] GenerateRawPCM(SoundType type)
        {
            float[] samples = type switch
            {
                SoundType.PlayerShotLight => GeneratePlayerShotLight(),
                SoundType.PlayerShotHeavy => GeneratePlayerShotHeavy(),
                SoundType.PlayerChargeUp => GeneratePlayerChargeUp(),
                SoundType.PlayerChargeRelease => GeneratePlayerChargeRelease(),
                SoundType.DashWhoosh => GenerateDashWhoosh(),
                SoundType.SpecialNova => GenerateSpecialNova(),
                SoundType.PowerupCollect => GeneratePowerupCollect(),
                SoundType.SoulPickup => GenerateSoulPickup(),

                SoundType.EnemyShotPlasma => GenerateEnemyShotPlasma(),
                SoundType.EnemyShotHeavy => GenerateEnemyShotHeavy(),
                SoundType.BossEnergyBlast => GenerateBossEnergyBlast(),
                SoundType.BossChargeUp => GenerateBossChargeUp(),
                SoundType.BossAttackTelegraph => GenerateBossAttackTelegraph(),
                SoundType.StatusSlowApplied => GenerateStatusSlowApplied(),
                SoundType.StatusPoisonApplied => GenerateStatusPoisonApplied(),

                // Faction Audio Signatures (12 dedicated procedural DSP types)
                SoundType.ToxisSpitBioAcid => GenerateToxisSpitBioAcid(),
                SoundType.ToxisChargeWarn => GenerateToxisChargeWarn(),
                SoundType.ToxisDeathExplosion => GenerateToxisDeathExplosion(),

                SoundType.KyrosCrystalResonance => GenerateKyrosCrystalResonance(),
                SoundType.KyrosChargeWarn => GenerateKyrosChargeWarn(),
                SoundType.KyrosShatter => GenerateKyrosShatter(),

                SoundType.TitanIndustrialBass => GenerateTitanIndustrialBass(),
                SoundType.TitanChargeWarn => GenerateTitanChargeWarn(),
                SoundType.TitanDestructionSub => GenerateTitanDestructionSub(),

                SoundType.ChronosTachyonGlitch => GenerateChronosTachyonGlitch(),
                SoundType.ChronosPhaseWarn => GenerateChronosPhaseWarn(),
                SoundType.ChronosWarpCollapse => GenerateChronosWarpCollapse(),

                SoundType.ExplosionSmall => GenerateExplosion(ExplosionScale.Small),
                SoundType.ExplosionMedium => GenerateExplosion(ExplosionScale.Medium),
                SoundType.ExplosionHeavy => GenerateExplosion(ExplosionScale.Heavy),
                SoundType.ExplosionMegaChain => GenerateMegaChainExplosion(),

                SoundType.UiHover => GenerateUiHover(),
                SoundType.UiSelect => GenerateUiSelect(),
                SoundType.UiBack => GenerateUiBack(),
                SoundType.UiUpgradePurchased => GenerateUiUpgradePurchased(),

                SoundType.ImpactShield => GenerateImpactShield(),
                SoundType.ImpactHull => GenerateImpactHull(),
                SoundType.CriticalHitPunch => GenerateCriticalHitPunch(),

                SoundType.BlackHoleDrone => GenerateBlackHoleDrone(),
                SoundType.ContinuousDamagePulse => GenerateContinuousDamagePulse(),

                SoundType.EmpBlast => GenerateEmpBlast(),
                SoundType.WaveAlert => GenerateWaveAlert(),
                SoundType.RockHit => GenerateRockHit(),
                SoundType.AcidSizzle => GenerateAcidSizzle(),
                SoundType.RamArtifactDrop => GenerateRamArtifactDrop(),

                _ => GeneratePlayerShotLight()
            };

            return ConvertFloatsToPCM16(samples);
        }

        /// <summary>
        /// 1. PlayerShotLight: Tiro balístico realista de alta cadência.
        /// Ruído rosa e marrom combinados com modulação de amplitude rápida (38ms),
        /// filtro passa-baixa descendente (350Hz -> 75Hz) e sub-thump encorpado (60Hz -> 35Hz em 20ms).
        /// Zero chirp senoidal agudo ou ressonância infantil 8-bit.
        /// </summary>
        private static float[] GeneratePlayerShotLight()
        {
            int count = (int)(SampleRate * 0.038f); // 38ms
            float[] buffer = new float[count];
            var rng = new Random(101);

            float[] pink = new float[count];
            GeneratePinkNoise(pink, rng, 0.50f);

            float[] brown = new float[count];
            GenerateBrownNoise(brown, rng, 0.65f);

            float[] noise = new float[count];
            for (int i = 0; i < count; i++)
            {
                noise[i] = pink[i] * 0.4f + brown[i] * 0.6f;
            }

            // Filtro passa-baixa abafado sem agudos estalados
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 350f, endCutoff: 75f, q: 0.707f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Modulação de amplitude rápida (percussão pneumática/balística)
                float ampMod = MathF.Exp(-t / 0.016f);

                // Sub-thump encorpado na câmara de disparo (60Hz -> 35Hz em 20ms)
                float subFreq = MathF.Max(35f, 60f - (t / 0.020f) * 25f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.012f) * 0.70f;

                buffer[i] = (noise[i] * ampMod) + subSine;
            }

            ApplyEnvelope(buffer, attackSec: 0.0015f, decaySec: 0.024f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.4f, masterGain: 0.85f);
            return buffer;
        }

        /// <summary>
        /// 2. PlayerShotHeavy: Tiro balístico pesado de alto calibre com corpo encorpado ("thump").
        /// Ruído marrom com modulação de amplitude rápida (55ms), filtro passa-baixa descendente
        /// (280Hz -> 55Hz) e sub-grave potente saturado (50Hz -> 25Hz em 35ms).
        /// Zero chirp senoidal agudo ou ressonância infantil 8-bit.
        /// </summary>
        private static float[] GeneratePlayerShotHeavy()
        {
            int count = (int)(SampleRate * 0.055f); // 55ms
            float[] buffer = new float[count];
            var rng = new Random(202);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.90f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 280f, endCutoff: 55f, q: 0.85f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Modulação de amplitude percussiva
                float ampMod = MathF.Exp(-t / 0.022f);

                // Sub-grave mecânico pesado (50Hz -> 25Hz em 35ms)
                float subFreq = MathF.Max(25f, 50f - (t / 0.035f) * 25f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.020f) * 0.85f;

                buffer[i] = (noise[i] * ampMod) + subSine;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.038f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.65f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// PlayerChargeUp: Síntese DSP com Tanh mastering para o som contínuo e crescente de carga de tiro.
        /// Varredura harmônica ascendente com sub-bass modulado e ressonância de plasma (120Hz -> 740Hz).
        /// </summary>
        private static float[] GeneratePlayerChargeUp()
        {
            int count = (int)(SampleRate * 0.400f); // 400ms por pulso de carga
            float[] buffer = new float[count];
            var rng = new Random(404);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.45f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 180f, endCutoff: 950f, q: 1.2f);

            float phase1 = 0f;
            float phase2 = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count; // 0.0 -> 1.0

                // Frequência harmônica ascendente exponencial
                float freq1 = 120f + progress * progress * 620f; // 120Hz -> 740Hz
                float freq2 = freq1 * 1.5f; // Quinta harmônica para textura sci-fi

                phase1 += 2f * MathF.PI * freq1 / SampleRate;
                phase2 += 2f * MathF.PI * freq2 / SampleRate;

                float sine1 = MathF.Sin(phase1);
                float sine2 = MathF.Sin(phase2) * 0.35f;

                // Sub-grave pulsante na base
                float subSine = MathF.Sin(2f * MathF.PI * 55f * t) * (0.3f + progress * 0.5f);

                // Modulação de amplitude crescente (crescendo suave)
                float amp = MathF.Pow(progress, 0.75f) * 0.85f;

                buffer[i] = ((sine1 + sine2) * 0.60f + noise[i] * 0.35f + subSine * 0.45f) * amp;
            }

            ApplyEnvelope(buffer, attackSec: 0.015f, decaySec: 0.060f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.6f, masterGain: 0.85f);
            return buffer;
        }

        /// <summary>
        /// PlayerChargeRelease: Síntese DSP com Tanh mastering para o impacto estrondoso da liberação do Super-Tiro Carregado.
        /// Transiente de alta energia, sub-bass sísmico (90Hz -> 24Hz) e corpo de plasma saturado.
        /// </summary>
        private static float[] GeneratePlayerChargeRelease()
        {
            int count = (int)(SampleRate * 0.280f); // 280ms
            float[] buffer = new float[count];
            var rng = new Random(505);

            float[] brown = new float[count];
            GenerateBrownNoise(brown, rng, 0.95f);
            ApplyTimeVaryingLpf(brown, SampleRate, startCutoff: 450f, endCutoff: 45f, q: 0.90f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Transiente de impacto rápido
                float impactPunch = MathF.Exp(-t / 0.018f) * 1.2f;

                // Sub-grave massivo descendente (90Hz -> 24Hz em 50ms)
                float subFreq = MathF.Max(24f, 90f - (t / 0.050f) * 66f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.045f) * 1.1f;

                // Saturação e cauda de plasma
                float plasmaTail = MathF.Sin(2f * MathF.PI * 140f * t) * MathF.Exp(-t / 0.090f) * 0.45f;

                buffer[i] = (brown[i] * impactPunch) + subSine + plasmaTail;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.085f, SampleRate);
            ApplyTanhMastering(buffer, drive: 2.2f, masterGain: 0.92f);
            return buffer;
        }

        /// <summary>
        /// 3. DashWhoosh: Varredura de ar abafada e rápida (ruído marrom com sweep LPF de 160Hz -> 80Hz).
        /// </summary>
        private static float[] GenerateDashWhoosh()
        {
            int count = (int)(SampleRate * 0.120f); // 120ms
            float[] buffer = new float[count];
            var rng = new Random(303);

            GenerateBrownNoise(buffer, rng, 0.90f);
            ApplyTimeVaryingLpf(buffer, SampleRate, startCutoff: 160f, endCutoff: 80f, q: 0.707f);
            ApplyEnvelope(buffer, attackSec: 0.004f, decaySec: 0.070f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.2f, masterGain: 0.75f);
            return buffer;
        }

        /// <summary>
        /// 4. SpecialNova: Pulso subsônico profundo (45Hz -> 20Hz) com sub-bass expansivo e ruído abafado.
        /// </summary>
        private static float[] GenerateSpecialNova()
        {
            int count = (int)(SampleRate * 0.350f); // 350ms
            float[] buffer = new float[count];
            var rng = new Random(404);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.70f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 180f, q: 0.8f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float subFreq = MathF.Max(20f, 45f - (t / 0.25f) * 25f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.18f) * 0.95f;
                buffer[i] = noise[i] + subSine;
            }

            ApplyEnvelope(buffer, attackSec: 0.004f, decaySec: 0.200f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.8f, masterGain: 0.92f);
            return buffer;
        }

        /// <summary>
        /// 5. PowerupCollect: Ressonância harmônica de baixa frequência (120Hz -> 180Hz) + ruído aveludado.
        /// </summary>
        private static float[] GeneratePowerupCollect()
        {
            int count = (int)(SampleRate * 0.120f); // 120ms
            float[] buffer = new float[count];
            var rng = new Random(505);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.50f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 240f, q: 0.707f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float f1 = 120f + (t / 0.12f) * 60f;
                float f2 = f1 * 1.5f;
                float tone = (MathF.Sin(2f * MathF.PI * f1 * t) * 0.5f + MathF.Sin(2f * MathF.PI * f2 * t) * 0.3f) * MathF.Exp(-t / 0.07f);
                buffer[i] = noise[i] + tone;
            }

            ApplyEnvelope(buffer, attackSec: 0.003f, decaySec: 0.080f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.15f, masterGain: 0.78f);
            return buffer;
        }

        /// <summary>
        /// 6. SoulPickup: Sopro etéreo ultrassuave (Velvet Breath).
        /// Ruído marrom filtrado estritamente por LPF de 240Hz com fade-in suave de 4ms, decaimento de 45ms,
        /// volume atenuado em -6dB (0.45f). Zero bips ou senoides agudas.
        /// </summary>
        private static float[] GenerateSoulPickup()
        {
            int count = (int)(SampleRate * 0.055f); // 55ms
            float[] buffer = new float[count];
            var rng = new Random(606);

            GenerateBrownNoise(buffer, rng, 0.45f); // Ganho base atenuado em -6dB
            ApplyBiquadLpf(buffer, SampleRate, cutoff: 240f, q: 0.707f);
            ApplyEnvelope(buffer, attackSec: 0.004f, decaySec: 0.045f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.0f, masterGain: 0.45f);
            return buffer;
        }

        /// <summary>
        /// 7. EnemyShotPlasma: Disparo de plasma pesado e abafado com "splash" encorpado.
        /// Ruído rosa e marrom com modulação de amplitude rápida (42ms), filtro passa-baixa descendente
        /// (320Hz -> 80Hz) e sub-thump de ionização. Zero tom senoidal infantil ou "piu-piu" 8-bit.
        /// </summary>
        private static float[] GenerateEnemyShotPlasma()
        {
            int count = (int)(SampleRate * 0.042f); // 42ms
            float[] buffer = new float[count];
            var rng = new Random(707);

            float[] pink = new float[count];
            GeneratePinkNoise(pink, rng, 0.55f);

            float[] brown = new float[count];
            GenerateBrownNoise(brown, rng, 0.50f);

            float[] noise = new float[count];
            for (int i = 0; i < count; i++)
            {
                noise[i] = pink[i] * 0.45f + brown[i] * 0.55f;
            }

            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 320f, endCutoff: 80f, q: 0.85f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Modulação de amplitude de splash
                float ampMod = MathF.Exp(-t / 0.018f);

                // Sub-thump de ionização e expansão de plasma (65Hz -> 30Hz em 20ms)
                float subFreq = MathF.Max(30f, 65f - (t / 0.020f) * 35f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.014f) * 0.65f;

                buffer[i] = (noise[i] * ampMod) + subSine;
            }

            ApplyEnvelope(buffer, attackSec: 0.0018f, decaySec: 0.028f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.35f, masterGain: 0.82f);
            return buffer;
        }

        /// <summary>
        /// 8. EnemyShotHeavy: Projétil pesado alienígena com impacto abafado e corpo encorpado ("splash/thump").
        /// Ruído marrom com modulação de amplitude rápida (60ms), filtro passa-baixa dinâmico (240Hz -> 50Hz)
        /// e sub-grave potente (55Hz -> 28Hz em 35ms). Zero chiado senoidal ou ressonância 8-bit.
        /// </summary>
        private static float[] GenerateEnemyShotHeavy()
        {
            int count = (int)(SampleRate * 0.060f); // 60ms
            float[] buffer = new float[count];
            var rng = new Random(808);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.85f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 240f, endCutoff: 50f, q: 0.85f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Modulação percussiva rápida
                float ampMod = MathF.Exp(-t / 0.025f);

                // Sub-thump pesado descendente (55Hz -> 28Hz em 35ms)
                float subFreq = MathF.Max(28f, 55f - (t / 0.035f) * 27f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.022f) * 0.80f;

                buffer[i] = (noise[i] * ampMod) + subSine;
            }

            ApplyEnvelope(buffer, attackSec: 0.0022f, decaySec: 0.040f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.55f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// CriticalHitPunch: Impacto seco, metálico e estalado (dry metallic impact crack).
        /// Ataque de 0.8ms, ruído marrom/rosa com LPF descendo rápido (800Hz -> 80Hz em 30ms),
        /// sub-thump mecânico saturado de 60Hz -> 30Hz em 25ms, distorção Tanh. Zero cauda senoidal ou bip agudo.
        /// </summary>
        private static float[] GenerateCriticalHitPunch()
        {
            int count = (int)(SampleRate * 0.040f); // 40ms
            float[] buffer = new float[count];
            var rng = new Random(909);

            float[] pinkNoise = new float[count];
            GeneratePinkNoise(pinkNoise, rng, 0.65f);

            float[] brownNoise = new float[count];
            GenerateBrownNoise(brownNoise, rng, 0.55f);

            // Combina ruídos rosa e marrom para corpo metálico seco sem chiado agudo
            float[] noise = new float[count];
            for (int i = 0; i < count; i++)
            {
                noise[i] = pinkNoise[i] * 0.55f + brownNoise[i] * 0.45f;
            }

            // LPF descendo rápido: 800Hz -> 80Hz em 30ms
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 800f, endCutoff: 80f, q: 1.2f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Sub-thump mecânico saturado de 60Hz -> 30Hz em 25ms
                float subFreq = MathF.Max(30f, 60f - (t / 0.025f) * 30f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.012f) * 0.85f;

                buffer[i] = noise[i] + subSine;
            }

            // Ataque rápido de 0.8ms, decaimento enxuto de 25ms, corte estrito de cauda
            ApplyEnvelope(buffer, attackSec: 0.0008f, decaySec: 0.025f, SampleRate);
            ApplyTanhMastering(buffer, drive: 2.0f, masterGain: 0.92f);
            return buffer;
        }

        /// <summary>
        /// 9. BossEnergyBlast: Sub-grave profundo (80Hz -> 32Hz) com pulso ressonante (260Hz -> 50Hz).
        /// </summary>
        private static float[] GenerateBossEnergyBlast()
        {
            int count = (int)(SampleRate * 0.280f); // 280ms
            float[] buffer = new float[count];
            var rng = new Random(999);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.65f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 260f, endCutoff: 50f, q: 1.8f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float subFreq = MathF.Max(32f, 80f - (t / 0.20f) * 48f);
                float sub = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.12f) * 0.90f;
                buffer[i] = noise[i] + sub;
            }

            ApplyEnvelope(buffer, attackSec: 0.003f, decaySec: 0.160f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.7f, masterGain: 0.92f);
            return buffer;
        }

        #region Faction Audio DSP Synthesis (Toxis, Kyros, Titans/Caldera, Chronos/Umbra)

        /// <summary>
        /// Toxis: Bio-acid spit. Viscous bubbling, organic squelch using frequency modulation
        /// (fc=220Hz->95Hz, fm=48Hz), pink noise burst filtered in 520Hz BPF, and micro-bubble pop (420Hz).
        /// </summary>
        private static float[] GenerateToxisSpitBioAcid()
        {
            int count = (int)(SampleRate * 0.055f); // 55ms = 2425 samples
            float[] buffer = new float[count];
            var rng = new Random(710);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.70f);
            ApplyBiquadBpf(noise, SampleRate, centerFreq: 520f, q: 3.2f);

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;

                // Moduladora FM (squelch orgânico viscoso)
                float fc = MathHelper.Lerp(220f, 95f, progress);
                float fm = 48f;
                float modIndex = 3.5f * MathF.Exp(-t / 0.025f);
                float mod = modIndex * MathF.Sin(2f * MathF.PI * fm * t);

                phase += 2f * MathF.PI * fc / SampleRate;
                float carrier = MathF.Sin(phase + mod) * 0.70f;

                // Micro-bolha percussiva de cavitação rápida (420Hz, decaimento 8ms)
                float bubble = MathF.Sin(2f * MathF.PI * 420f * t) * MathF.Exp(-t / 0.008f) * 0.45f;

                buffer[i] = carrier + (noise[i] * 0.45f) + bubble;
            }

            ApplyEnvelope(buffer, attackSec: 0.0015f, decaySec: 0.035f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.4f, masterGain: 0.82f);
            return buffer;
        }

        /// <summary>
        /// Toxis: Bio-acid charge telegraph / warning. Sizzling chemical effervescence with
        /// 35Hz amplitude modulation tremolo and viscous rising glissando tone (110Hz->490Hz).
        /// </summary>
        private static float[] GenerateToxisChargeWarn()
        {
            int count = (int)(SampleRate * 0.240f); // 240ms = 10584 samples
            float[] buffer = new float[count];
            var rng = new Random(711);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.85f);
            ApplyBiquadBpf(noise, SampleRate, centerFreq: 650f, q: 2.8f);

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;

                // Tremolo de efervescência química a 35Hz
                float tremolo = 0.5f + (0.5f * MathF.Sin(2f * MathF.PI * 35f * t));
                float sizzle = noise[i] * tremolo * 0.65f;

                // Glissando ascendente viscoso (110Hz -> 490Hz)
                float freq = 110f + MathF.Pow(progress, 1.5f) * 380f;
                phase += 2f * MathF.PI * freq / SampleRate;
                float tone = MathF.Sin(phase) * (0.20f + 0.80f * progress) * 0.55f;

                buffer[i] = sizzle + tone;
            }

            ApplyEnvelope(buffer, attackSec: 0.180f, decaySec: 0.040f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.35f, masterGain: 0.85f);
            return buffer;
        }

        /// <summary>
        /// Toxis: Death explosion / cocoon rupture. Membranous rupture pop, thick liquid acid dispersion,
        /// saturated brown noise burst, and organic decompression sub-thump (75Hz->30Hz).
        /// </summary>
        private static float[] GenerateToxisDeathExplosion()
        {
            int count = (int)(SampleRate * 0.320f); // 320ms = 14112 samples
            float[] buffer = new float[count];
            var rng = new Random(712);

            float[] brownNoise = new float[count];
            GenerateBrownNoise(brownNoise, rng, 0.90f);
            ApplyBiquadBpf(brownNoise, SampleRate, centerFreq: 400f, q: 1.8f);

            float[] pinkNoise = new float[count];
            GeneratePinkNoise(pinkNoise, rng, 0.70f);
            ApplyBiquadBpf(pinkNoise, SampleRate, centerFreq: 850f, q: 2.5f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Sub-grave orgânico de descompressão
                float subFreq = MathF.Max(30f, 75f - (t / 0.15f) * 45f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.10f) * 0.85f;

                // Estalo de rompimento de membrana biológica
                float pop = (t < 0.015f) ? MathF.Sin(2f * MathF.PI * 180f * t) * (1f - (t / 0.015f)) * 0.60f : 0f;

                buffer[i] = (brownNoise[i] * 0.75f) + (pinkNoise[i] * 0.45f) + subSine + pop;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.120f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.6f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// Kyros: High-Q crystal chime, resonant piezoelectric ring modulation with clear harmonics
        /// (f1=1320Hz, f2=1980Hz, f3=2640Hz), initial quartz friction HPF transient, and ring modulation.
        /// </summary>
        private static float[] GenerateKyrosCrystalResonance()
        {
            int count = (int)(SampleRate * 0.065f); // 65ms = 2866 samples
            float[] buffer = new float[count];
            var rng = new Random(720);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.60f);
            ApplyBiquadHpf(noise, SampleRate, cutoff: 3200f, q: 1.5f);

            float phase1 = 0f, phase2 = 0f, phase3 = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Pitch-bite inicial exponencial em quartzo (+180Hz nos primeiros 5ms)
                float bite = 180f * MathF.Exp(-t / 0.005f);

                phase1 += 2f * MathF.PI * (1320f + bite) / SampleRate;
                phase2 += 2f * MathF.PI * (1980f + bite * 1.5f) / SampleRate;
                phase3 += 2f * MathF.PI * (2640f + bite * 2.0f) / SampleRate;

                float chime = (MathF.Sin(phase1) * 0.55f) + (MathF.Sin(phase2) * 0.35f) + (MathF.Sin(phase3) * 0.25f);

                // Modulação piezoelétrica sutil (110Hz)
                float piezo = 0.80f + 0.20f * MathF.Cos(2f * MathF.PI * 110f * t);

                // Transiente vítreo de atrito inicial (decaimento 6ms)
                float friction = noise[i] * MathF.Exp(-t / 0.006f) * 0.40f;

                buffer[i] = (chime * piezo) + friction;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.048f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.2f, masterGain: 0.80f);
            return buffer;
        }

        /// <summary>
        /// Kyros: Prismatic charge warning. Resonant crystalline acoustic beating (7Hz delta) between
        /// dual carrier tones sweeping upward (980Hz->2180Hz) with shimmer octave (2x) and icy whisper.
        /// </summary>
        private static float[] GenerateKyrosChargeWarn()
        {
            int count = (int)(SampleRate * 0.260f); // 260ms = 11466 samples
            float[] buffer = new float[count];
            var rng = new Random(721);

            float[] whisper = new float[count];
            GeneratePinkNoise(whisper, rng, 0.40f);
            ApplyBiquadHpf(whisper, SampleRate, cutoff: 4000f, q: 1.2f);

            float phaseA = 0f, phaseB = 0f, phaseShimmer = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;

                // Frequências com batimento acústico de 7Hz
                float fA = 980f + MathF.Pow(progress, 2f) * 1200f;
                float fB = fA + 7f;
                float fShimmer = fA * 2f;

                phaseA += 2f * MathF.PI * fA / SampleRate;
                phaseB += 2f * MathF.PI * fB / SampleRate;
                phaseShimmer += 2f * MathF.PI * fShimmer / SampleRate;

                float beatingTone = (MathF.Sin(phaseA) * 0.45f) + (MathF.Sin(phaseB) * 0.45f) + (MathF.Sin(phaseShimmer) * 0.20f);

                buffer[i] = beatingTone + (whisper[i] * 0.20f);
            }

            ApplyEnvelope(buffer, attackSec: 0.210f, decaySec: 0.035f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.25f, masterGain: 0.84f);
            return buffer;
        }

        /// <summary>
        /// Kyros: Complete crystalline shatter / destruction. Cascade fragmentation of ice and quartz crystals
        /// with 4 staggered resonant modal frequencies (1760, 2489, 3520, 4978Hz) and high-pass glass crack.
        /// </summary>
        private static float[] GenerateKyrosShatter()
        {
            int count = (int)(SampleRate * 0.380f); // 380ms = 16758 samples
            float[] buffer = new float[count];
            var rng = new Random(722);

            float[] glassCrack = new float[count];
            GeneratePinkNoise(glassCrack, rng, 0.85f);
            ApplyBiquadHpf(glassCrack, SampleRate, cutoff: 2800f, q: 2.0f);

            float[] modalFreqs = { 1760f, 2489f, 3520f, 4978f };
            float[] offsets = { 0.000f, 0.012f, 0.025f, 0.040f };
            float[] amplitudes = { 0.50f, 0.40f, 0.30f, 0.25f };

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float modalSum = 0f;

                for (int m = 0; m < modalFreqs.Length; m++)
                {
                    if (t >= offsets[m])
                    {
                        float delta = t - offsets[m];
                        modalSum += MathF.Sin(2f * MathF.PI * modalFreqs[m] * delta) * MathF.Exp(-delta / 0.080f) * amplitudes[m];
                    }
                }

                // Fragmentação vítrea inicial
                float crack = glassCrack[i] * MathF.Exp(-t / 0.045f) * 0.80f;

                buffer[i] = modalSum + crack;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.150f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.3f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// Titans & Caldera: Industrial sub-bass cannon. Heavy sub-bass punch (65Hz->26Hz),
        /// dense biquad-lowpassed brown noise compression body (160Hz), and mechanical steel breech click (320Hz->140Hz).
        /// </summary>
        private static float[] GenerateTitanIndustrialBass()
        {
            int count = (int)(SampleRate * 0.085f); // 85ms = 3748 samples
            float[] buffer = new float[count];
            var rng = new Random(730);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.85f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 160f, q: 1.2f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Sub-grave maciço descendente (65Hz -> 26Hz em 50ms)
                float subFreq = MathF.Max(26f, 65f - (t / 0.050f) * 39f);
                float subSine = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.045f) * 0.85f;

                // Transiente metálico de percussor de aço da culatra (320Hz -> 140Hz em 12ms)
                float clickFreq = MathF.Max(140f, 320f - (t / 0.012f) * 180f);
                float click = MathF.Sin(2f * MathF.PI * clickFreq * t) * MathF.Exp(-t / 0.012f) * 0.50f;

                buffer[i] = subSine + (noise[i] * 0.80f) + click;
            }

            ApplyEnvelope(buffer, attackSec: 0.0015f, decaySec: 0.055f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.85f, masterGain: 0.92f);
            return buffer;
        }

        /// <summary>
        /// Titans & Caldera: Forge pressurization / turbine spin-up warning. Rising sub-rumble (30Hz->105Hz),
        /// swept bandpass noise (180Hz->850Hz), and cyclic 18Hz pneumatic combustion chamber rotation.
        /// </summary>
        private static float[] GenerateTitanChargeWarn()
        {
            int count = (int)(SampleRate * 0.320f); // 320ms = 14112 samples
            float[] buffer = new float[count];
            var rng = new Random(731);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.80f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 180f, endCutoff: 850f, q: 2.0f);

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;

                // Sub-rumble ascendente de forja
                float rumbleFreq = 30f + MathF.Pow(progress, 1.2f) * 75f;
                phase += 2f * MathF.PI * rumbleFreq / SampleRate;
                float rumble = MathF.Sin(phase) * (0.3f + 0.7f * progress) * 0.65f;

                // Modulação cíclica pneumática a 18Hz
                float pneumaticPulse = 0.60f + 0.40f * MathF.Sin(2f * MathF.PI * 18f * t);

                buffer[i] = rumble + (noise[i] * pneumaticPulse * 0.60f);
            }

            ApplyEnvelope(buffer, attackSec: 0.260f, decaySec: 0.045f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.5f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// Titans & Caldera: Heavy titan destruction / structural seismic collapse.
        /// Subsonic seismic rumble (38Hz), heavily driven brown noise, and metallic tearing clatter (650Hz BPF).
        /// </summary>
        private static float[] GenerateTitanDestructionSub()
        {
            int count = (int)(SampleRate * 0.450f); // 450ms = 19845 samples
            float[] buffer = new float[count];
            var rng = new Random(732);

            float[] brownNoise = new float[count];
            GenerateBrownNoise(brownNoise, rng, 0.90f);
            ApplyBiquadLpf(brownNoise, SampleRate, cutoff: 200f, q: 1.0f);

            float[] clatter = new float[count];
            GeneratePinkNoise(clatter, rng, 0.75f);
            ApplyBiquadBpf(clatter, SampleRate, centerFreq: 650f, q: 3.0f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Oscilação sísmica sub-sônica profunda (38Hz, decaimento longo)
                float seismic = MathF.Sin(2f * MathF.PI * 38f * t) * MathF.Exp(-t / 0.250f) * 0.90f;

                // Estalos de blindagem de aço rachando e se deformando
                float metalTear = clatter[i] * MathF.Exp(-t / 0.120f) * 0.60f;

                buffer[i] = seismic + (brownNoise[i] * 0.85f) + metalTear;
            }

            ApplyEnvelope(buffer, attackSec: 0.003f, decaySec: 0.220f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.7f, masterGain: 0.94f);
            return buffer;
        }

        /// <summary>
        /// Chronos & Umbra: Tachyon glitch beam. Ring modulation (fc=440Hz, fm=713Hz) generating inharmonic sidebands,
        /// rapid 85Hz square-wave amplitude gating (stutter), bitcrushing decimation, and sweep LPF (2200Hz->250Hz).
        /// </summary>
        private static float[] GenerateChronosTachyonGlitch()
        {
            int count = (int)(SampleRate * 0.060f); // 60ms = 2646 samples
            float[] buffer = new float[count];
            var rng = new Random(740);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.60f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 2200f, endCutoff: 250f, q: 1.5f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Ring Modulation: parciais em |440 - 713| = 273Hz e 440 + 713 = 1153Hz
                float carrier = MathF.Sin(2f * MathF.PI * 440f * t);
                float modulator = MathF.Sin(2f * MathF.PI * 713f * t);
                float ring = carrier * modulator;

                // Gate rápido de onda quadrada a 85Hz (stutter / descontinuidade temporal)
                float gate = (MathF.Sin(2f * MathF.PI * 85f * t) > 0f) ? 1.0f : 0.25f;

                float rawSample = (ring * gate * 0.70f) + (noise[i] * 0.35f);

                // Bitcrush / Decimação (quantização em 16 degraus)
                buffer[i] = MathF.Round(rawSample * 8.0f) / 8.0f;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.040f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.45f, masterGain: 0.84f);
            return buffer;
        }

        /// <summary>
        /// Chronos & Umbra: Quantum phase warp warning. Tritone dissonant pair (330Hz and 467Hz),
        /// chaotic 12Hz phase warble jitter, and reverse swell crescendo envelope.
        /// </summary>
        private static float[] GenerateChronosPhaseWarn()
        {
            int count = (int)(SampleRate * 0.250f); // 250ms = 11025 samples
            float[] buffer = new float[count];
            var rng = new Random(741);

            float phase1 = 0f, phase2 = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Warble taquiônico de fase (LFO a 12Hz + jitter estocástico)
                float jitter = ((float)rng.NextDouble() - 0.5f) * 0.20f;
                float warble = 0.50f * MathF.Sin(2f * MathF.PI * 12f * t) + jitter;

                phase1 += 2f * MathF.PI * 330f / SampleRate;
                phase2 += 2f * MathF.PI * 467f / SampleRate;

                float sine1 = MathF.Sin(phase1 + warble);
                float sine2 = MathF.Sin(phase2 - warble);

                // Reverse swell crescendo exponencial
                float swell = MathF.Exp(3.0f * (t - 0.250f));

                // Micro-estalo quântico estocástico
                float crackle = (rng.NextDouble() < 0.03) ? ((float)rng.NextDouble() - 0.5f) * 0.30f : 0f;

                buffer[i] = ((sine1 + sine2) * 0.50f * swell) + crackle;
            }

            ApplyEnvelope(buffer, attackSec: 0.220f, decaySec: 0.020f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.4f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// Chronos & Umbra: Gravitational warp collapse / dimensional rift.
        /// Vacuum collapse pulse (50Hz + 56Hz with midpoint polarity cancellation),
        /// bitcrushed void noise burst, and sub-bass gravitational implosion (80Hz->20Hz).
        /// </summary>
        private static float[] GenerateChronosWarpCollapse()
        {
            int count = (int)(SampleRate * 0.340f); // 340ms = 14994 samples
            float[] buffer = new float[count];
            var rng = new Random(742);

            float[] pinkNoise = new float[count];
            GeneratePinkNoise(pinkNoise, rng, 0.85f);
            ApplyBiquadBpf(pinkNoise, SampleRate, centerFreq: 580f, q: 2.0f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Pulso de vácuo gravitacional: senoides de 50Hz e 56Hz com inversão de polaridade no centro
                float polarity = (t < 0.170f) ? 1.0f : -1.0f;
                float vacuumPulse = (MathF.Sin(2f * MathF.PI * 50f * t) + MathF.Sin(2f * MathF.PI * 56f * t)) * 0.50f * polarity;

                // Sub-grave de implosão gravitacional (80Hz -> 20Hz)
                float subFreq = MathF.Max(20f, 80f - (t / 0.200f) * 60f);
                float subThump = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.180f) * 0.75f;

                // Burst de ruído quantizado (bitcrushed void flutter)
                float quantNoise = MathF.Floor(pinkNoise[i] * 6.0f) / 6.0f * MathF.Exp(-t / 0.140f);

                buffer[i] = vacuumPulse * 0.60f + subThump + quantNoise * 0.50f;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.160f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.5f, masterGain: 0.88f);
            return buffer;
        }

        #endregion

        #region Multi-Band Explosion Layering DSP

        /// <summary>
        /// Síntese de Explosão Cinematográfica HD (Volume Alto, Presença Máxima, Punch Perfurante e Sub-Rumble Encorpado):
        /// - Camada 1: Transiente de Ataque Perfurante (Pitch-drop punch 1050Hz -> 200Hz + air-snap BPF 1300Hz).
        /// - Camada 2: Corpo de Expansão e Chamas Denso (Ruído Rosa com varredura BPF 950Hz -> 220Hz + ressonância metálica).
        /// - Camada 3: Sub-Thump Potente (Senoide 130Hz -> 48Hz com peso total audível).
        /// Masterização com Makeup Gain e Limiter Tanh a 0.96f de pico para máxima audibilidade e impacto sem distorção estourada.
        /// </summary>
        public static float[] GenerateExplosion(ExplosionScale scale)
        {
            float durationSec = scale switch
            {
                ExplosionScale.Small => 0.240f,
                ExplosionScale.Medium => 0.420f,
                ExplosionScale.Heavy => 0.680f,
                _ => 0.420f
            };

            int count = (int)(SampleRate * durationSec);
            float[] mixed = new float[count];

            int seed = scale switch
            {
                ExplosionScale.Small => 1001,
                ExplosionScale.Medium => 1002,
                ExplosionScale.Heavy => 1003,
                _ => 1000
            };
            var rng = new Random(seed);

            // -------------------------------------------------------------
            // CAMADA 1: Transiente de Ataque / Impacto Inicial ("Punch & Crack")
            // -------------------------------------------------------------
            float[] band1Punch = new float[count];
            float punchStartFreq = scale == ExplosionScale.Heavy ? 850f : (scale == ExplosionScale.Small ? 1100f : 980f);
            float punchEndFreq = scale == ExplosionScale.Heavy ? 160f : (scale == ExplosionScale.Small ? 250f : 200f);
            float punchDecaySec = scale switch
            {
                ExplosionScale.Small => 0.045f,
                ExplosionScale.Medium => 0.065f,
                ExplosionScale.Heavy => 0.090f,
                _ => 0.065f
            };

            // Estalo de ar percussivo em passa-faixa audível
            float[] airSnap = new float[count];
            for (int i = 0; i < count; i++)
            {
                airSnap[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            ApplyBiquadBpf(airSnap, SampleRate, centerFreq: scale == ExplosionScale.Heavy ? 1000f : 1350f, q: 1.4f);
            ApplyEnvelope(airSnap, attackSec: 0.0005f, decaySec: punchDecaySec * 0.75f, SampleRate);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = Math.Min(1.0f, t / punchDecaySec);
                float curFreq = MathHelper.Lerp(punchStartFreq, punchEndFreq, MathF.Pow(p, 0.4f));
                float punchTone = MathF.Sin(2f * MathF.PI * curFreq * t) * MathF.Exp(-t / (punchDecaySec * 0.80f)) * 0.90f;
                band1Punch[i] = punchTone + (airSnap[i] * 0.75f);
            }
            ApplyEnvelope(band1Punch, attackSec: 0.0005f, decaySec: punchDecaySec, SampleRate);

            // -------------------------------------------------------------
            // CAMADA 2: Corpo Ressonante de Expansão (Ruído Médio-Grave Filtrado)
            // -------------------------------------------------------------
            float[] band2Body = new float[count];
            GeneratePinkNoise(band2Body, rng, 1.0f);

            float startLpf = scale switch
            {
                ExplosionScale.Small => 950f,
                ExplosionScale.Medium => 820f,
                ExplosionScale.Heavy => 700f,
                _ => 820f
            };
            float endLpf = scale switch
            {
                ExplosionScale.Small => 250f,
                ExplosionScale.Medium => 190f,
                ExplosionScale.Heavy => 140f,
                _ => 190f
            };
            ApplyTimeVaryingLpf(band2Body, SampleRate, startCutoff: startLpf, endCutoff: endLpf, q: 0.95f);

            float bodyDecaySec = scale switch
            {
                ExplosionScale.Small => 0.140f,
                ExplosionScale.Medium => 0.250f,
                ExplosionScale.Heavy => 0.420f,
                _ => 0.250f
            };
            ApplyEnvelope(band2Body, attackSec: 0.002f, decaySec: bodyDecaySec, SampleRate);

            // -------------------------------------------------------------
            // CAMADA 3: Sub-Thump e Rumble Estrutural (Senoide 130Hz -> 48Hz)
            // -------------------------------------------------------------
            float[] band3Sub = new float[count];
            float startSubFreq = scale == ExplosionScale.Heavy ? 110f : (scale == ExplosionScale.Small ? 145f : 130f);
            float endSubFreq = scale == ExplosionScale.Heavy ? 38f : (scale == ExplosionScale.Small ? 60f : 48f);
            float subDecaySec = scale switch
            {
                ExplosionScale.Small => 0.180f,
                ExplosionScale.Medium => 0.350f,
                ExplosionScale.Heavy => 0.580f,
                _ => 0.350f
            };

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = Math.Min(1.0f, t / (durationSec * 0.85f));
                float currentFreq = MathHelper.Lerp(startSubFreq, endSubFreq, progress);
                band3Sub[i] = MathF.Sin(2f * MathF.PI * currentFreq * t) * 0.95f;
            }
            ApplyEnvelope(band3Sub, attackSec: 0.002f, decaySec: subDecaySec, SampleRate);

            // -------------------------------------------------------------
            // Somatório e Masterização de Alto Volume (Makeup Gain + Tanh Limiter)
            // -------------------------------------------------------------
            for (int i = 0; i < count; i++)
            {
                mixed[i] = (band1Punch[i] * 0.85f) + (band2Body[i] * 0.85f) + (band3Sub[i] * 0.80f);
            }

            ApplyTanhMastering(mixed, drive: 1.75f, masterGain: 0.96f);
            return mixed;
        }

        /// <summary>
        /// Síntese de Explosão MegaChain HD (Acúmulo Crítico de Detonações):
        /// Duplo pulso de impacto de choque + corpo expansivo estendido + batimento sub-grave duplo.
        /// </summary>
        public static float[] GenerateMegaChainExplosion()
        {
            int count = (int)(SampleRate * 0.850f); // 850ms
            float[] mixed = new float[count];
            var rng = new Random(1004);

            // 1. Transiente de impacto duplo potente
            float[] punch = new float[count];
            float[] airBurst = new float[count];
            for (int i = 0; i < count; i++)
            {
                airBurst[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            ApplyBiquadBpf(airBurst, SampleRate, centerFreq: 1100f, q: 1.3f);
            ApplyEnvelope(airBurst, attackSec: 0.0005f, decaySec: 0.090f, SampleRate);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float freq1 = MathF.Max(140f, 950f - (t / 0.060f) * 810f);
                float tone1 = MathF.Sin(2f * MathF.PI * freq1 * t) * MathF.Exp(-t / 0.050f) * 0.90f;

                // Segundo pulso de colapso a 45ms
                float t2 = t - 0.045f;
                float tone2 = 0f;
                if (t2 > 0f)
                {
                    float freq2 = MathF.Max(100f, 700f - (t2 / 0.070f) * 600f);
                    tone2 = MathF.Sin(2f * MathF.PI * freq2 * t2) * MathF.Exp(-t2 / 0.055f) * 0.80f;
                }

                punch[i] = tone1 + tone2 + (airBurst[i] * 0.70f);
            }
            ApplyEnvelope(punch, attackSec: 0.0005f, decaySec: 0.095f, SampleRate);

            // 2. Corpo expansivo de plasma com LPF dinâmico (900Hz -> 120Hz)
            float[] body = new float[count];
            GeneratePinkNoise(body, rng, 1.0f);
            ApplyTimeVaryingLpf(body, SampleRate, startCutoff: 900f, endCutoff: 120f, q: 0.95f);
            ApplyEnvelope(body, attackSec: 0.002f, decaySec: 0.480f, SampleRate);

            // 3. Batimento sub-grave duplo catastrófico (45Hz e 55Hz)
            float[] subRumble = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float toneA = MathF.Sin(2f * MathF.PI * 45f * t);
                float toneB = MathF.Sin(2f * MathF.PI * 55f * t);
                subRumble[i] = (toneA + toneB) * 0.65f;
            }
            ApplyEnvelope(subRumble, attackSec: 0.003f, decaySec: 0.750f, SampleRate);

            for (int i = 0; i < count; i++)
            {
                mixed[i] = (punch[i] * 0.80f) + (body[i] * 0.80f) + (subRumble[i] * 0.75f);
            }

            ApplyTanhMastering(mixed, drive: 1.80f, masterGain: 0.96f);
            return mixed;
        }

        #endregion

        #region Síntese de Interface Cristalina / Holográfica (UI)

        /// <summary>
        /// UiHover: Tick harmônico holográfico cristalino de alta frequência (1800Hz e 3600Hz somadas).
        /// Envelope percussivo estrito (1ms ataque, 12ms decay, 18ms total).
        /// </summary>
        public static float[] GenerateUiHover()
        {
            int count = (int)(SampleRate * 0.018f); // 18ms
            float[] buffer = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float tone1 = MathF.Sin(2f * MathF.PI * 1800f * t) * 0.60f;
                float tone2 = MathF.Sin(2f * MathF.PI * 3600f * t) * 0.40f;
                buffer[i] = tone1 + tone2;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.012f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.0f, masterGain: 0.35f);
            return buffer;
        }

        /// <summary>
        /// UiSelect: Acorde bitonal ascendente cristalino de confirmação (880Hz -> 1760Hz com harmônico em 3520Hz).
        /// Duração 55ms.
        /// </summary>
        public static float[] GenerateUiSelect()
        {
            int count = (int)(SampleRate * 0.055f); // 55ms
            float[] buffer = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / 0.055f;
                float currentFreq = MathHelper.Lerp(880f, 1760f, progress);
                float fundamental = MathF.Sin(2f * MathF.PI * currentFreq * t) * 0.70f;
                float overtone = MathF.Sin(2f * MathF.PI * (currentFreq * 2f) * t) * 0.25f;
                buffer[i] = fundamental + overtone;
            }

            ApplyEnvelope(buffer, attackSec: 0.0015f, decaySec: 0.050f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.0f, masterGain: 0.55f);
            return buffer;
        }

        /// <summary>
        /// UiBack: Sweep senoidal descendente suave de cancelamento/retorno (650Hz -> 320Hz).
        /// Duração 40ms.
        /// </summary>
        public static float[] GenerateUiBack()
        {
            int count = (int)(SampleRate * 0.040f); // 40ms
            float[] buffer = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / 0.040f;
                float currentFreq = MathHelper.Lerp(650f, 320f, progress);
                buffer[i] = MathF.Sin(2f * MathF.PI * currentFreq * t) * 0.80f;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.035f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.0f, masterGain: 0.45f);
            return buffer;
        }

        /// <summary>
        /// UiUpgradePurchased: Arpejo cristalino ultrarrápido de 3 tons puros (1046Hz, 1318Hz, 1568Hz — C6, E6, G6)
        /// com cauda de decaimento brilhante de 130ms.
        /// </summary>
        public static float[] GenerateUiUpgradePurchased()
        {
            int count = (int)(SampleRate * 0.130f); // 130ms
            float[] buffer = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Tom 1: C6 (1046.5Hz) a partir de 0ms
                float tone1 = t >= 0f ? MathF.Sin(2f * MathF.PI * 1046.5f * t) * MathF.Exp(-t / 0.08f) * 0.50f : 0f;

                // Tom 2: E6 (1318.5Hz) a partir de 22ms
                float tone2 = t >= 0.022f ? MathF.Sin(2f * MathF.PI * 1318.5f * (t - 0.022f)) * MathF.Exp(-(t - 0.022f) / 0.08f) * 0.55f : 0f;

                // Tom 3: G6 (1568.0Hz) a partir de 44ms
                float tone3 = t >= 0.044f ? MathF.Sin(2f * MathF.PI * 1568.0f * (t - 0.044f)) * MathF.Exp(-(t - 0.044f) / 0.08f) * 0.60f : 0f;

                // Reverb shimmer superior (3136Hz)
                float shimmer = t >= 0.044f ? MathF.Sin(2f * MathF.PI * 3136.0f * (t - 0.044f)) * MathF.Exp(-(t - 0.044f) / 0.06f) * 0.15f : 0f;

                buffer[i] = tone1 + tone2 + tone3 + shimmer;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.100f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.1f, masterGain: 0.65f);
            return buffer;
        }

        /// <summary>
        /// RamArtifactDrop: Efeito sonoro de tilintar cristalino/moeda metálica futurista ao dropar um Cerne de Memória.
        /// Duplo tom brilhante de sino senoidal harmônico (1174Hz [D6] seguido de 1760Hz [A6]) com transiente metálico límpido.
        /// </summary>
        private static float[] GenerateRamArtifactDrop()
        {
            int count = (int)(SampleRate * 0.120f); // 120ms
            float[] buffer = new float[count];
            var rng = new Random(4242);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // 1º sino (D6 fundamental 1174Hz)
                float bell1 = MathF.Sin(2f * MathF.PI * 1174f * t) * MathF.Exp(-t / 0.040f) * 0.55f;
                // Harmônico cristalino superior
                float bellHarmonic = MathF.Sin(2f * MathF.PI * 2348f * t) * MathF.Exp(-t / 0.025f) * 0.25f;

                // 2º sino ligeiramente atrasado (A6 1760Hz a 25ms) - Efeito de moeda metálica dupla
                float t2 = t - 0.025f;
                float bell2 = 0f;
                if (t2 > 0f)
                {
                    bell2 = MathF.Sin(2f * MathF.PI * 1760f * t2) * MathF.Exp(-t2 / 0.055f) * 0.70f;
                    bell2 += MathF.Sin(2f * MathF.PI * 3520f * t2) * MathF.Exp(-t2 / 0.030f) * 0.20f;
                }

                // Micro-chime de brilho metálico
                float sparkle = (float)(rng.NextDouble() * 2.0 - 1.0) * MathF.Exp(-t / 0.008f) * 0.15f;

                buffer[i] = bell1 + bellHarmonic + bell2 + sparkle;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.090f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.05f, masterGain: 0.75f);
            return buffer;
        }

        #endregion

        /// <summary>
        /// 10. ImpactShield: Deflexão de energia com pulso eletrostático brilhante (BPF 1400Hz + sweep 1500Hz -> 600Hz)
        /// somado à ressonância de campo de força (320Hz -> 140Hz) e sub-rumble de absorção.
        /// </summary>
        private static float[] GenerateImpactShield()
        {
            int count = (int)(SampleRate * 0.065f); // 65ms
            float[] buffer = new float[count];
            var rng = new Random(1313);

            // 1. Transiente de descarga eletrostática (alta definição audível)
            float[] transientNoise = new float[count];
            for (int i = 0; i < count; i++)
            {
                transientNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            ApplyBiquadBpf(transientNoise, SampleRate, centerFreq: 1400f, q: 1.8f);
            ApplyEnvelope(transientNoise, attackSec: 0.0005f, decaySec: 0.015f, SampleRate);

            // 2. Corpo ressonante do escudo (oscilação do campo de força)
            float[] forcefieldNoise = new float[count];
            GeneratePinkNoise(forcefieldNoise, rng, 0.50f);
            ApplyBiquadLpf(forcefieldNoise, SampleRate, cutoff: 420f, q: 0.9f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Zap descendente rápido
                float zapFreq = MathF.Max(600f, 1500f - (t / 0.015f) * 900f);
                float zapTone = MathF.Sin(2f * MathF.PI * zapFreq * t) * MathF.Exp(-t / 0.012f) * 0.45f;

                // Ressonância defletora
                float resFreq = MathF.Max(140f, 320f - (t / 0.045f) * 180f);
                float resTone = MathF.Sin(2f * MathF.PI * resFreq * t) * MathF.Exp(-t / 0.035f) * 0.65f;

                // Sub-rumble
                float subFreq = MathF.Max(70f, 110f - (t / 0.050f) * 40f);
                float subTone = MathF.Sin(2f * MathF.PI * subFreq * t) * MathF.Exp(-t / 0.040f) * 0.50f;

                buffer[i] = (transientNoise[i] * 0.70f) + (forcefieldNoise[i] * 0.40f) + zapTone + resTone + subTone;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.045f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.6f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// 11. ImpactHull: Crunch balístico percussivo nítido (BPF 1100Hz + saturação Tanh)
        /// com deformação de chapa metálica (440Hz -> 180Hz) e thud estrutural firme (120Hz -> 60Hz).
        /// </summary>
        private static float[] GenerateImpactHull()
        {
            int count = (int)(SampleRate * 0.055f); // 55ms
            float[] buffer = new float[count];
            var rng = new Random(1414);

            // 1. Transiente balístico percussivo ("clack / snap" metálico)
            float[] snapNoise = new float[count];
            for (int i = 0; i < count; i++)
            {
                snapNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            ApplyBiquadBpf(snapNoise, SampleRate, centerFreq: 1100f, q: 2.0f);
            for (int i = 0; i < count; i++)
            {
                snapNoise[i] = MathF.Tanh(snapNoise[i] * 2.8f);
            }
            ApplyEnvelope(snapNoise, attackSec: 0.0005f, decaySec: 0.012f, SampleRate);

            // 2. Corpo metálico e ruído estrutural
            float[] bodyNoise = new float[count];
            GeneratePinkNoise(bodyNoise, rng, 0.55f);
            ApplyBiquadLpf(bodyNoise, SampleRate, cutoff: 380f, q: 0.85f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Deformação de chapa metálica
                float metalFreq = MathF.Max(180f, 440f - (t / 0.035f) * 260f);
                float metalTone = MathF.Sin(2f * MathF.PI * metalFreq * t) * MathF.Exp(-t / 0.025f) * 0.65f;

                // Thud de massa física
                float thudFreq = MathF.Max(60f, 120f - (t / 0.040f) * 60f);
                float thudTone = MathF.Sin(2f * MathF.PI * thudFreq * t) * MathF.Exp(-t / 0.030f) * 0.60f;

                buffer[i] = (snapNoise[i] * 0.85f) + (bodyNoise[i] * 0.40f) + metalTone + thudTone;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.035f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.8f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// 12. BlackHoleDrone: Buffer PCM em loop contínuo contendo batimento senoidal de baixa frequência (42Hz e 47Hz)
        /// com LFO lento (0.4Hz) modulando a amplitude de ruído sub-grave (LPF 120Hz).
        /// Duração de 2.0s com continuidade de fase exata.
        /// </summary>
        private static float[] GenerateBlackHoleDrone()
        {
            int count = (int)(SampleRate * 2.0f); // 2.0s loop
            float[] buffer = new float[count];
            var rng = new Random(1111);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.60f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 120f, q: 0.707f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Batimento de 5Hz entre 42Hz e 47Hz
                float tone1 = MathF.Sin(2f * MathF.PI * 42f * t);
                float tone2 = MathF.Sin(2f * MathF.PI * 47f * t);
                float subBeat = (tone1 + tone2) * 0.40f;

                // LFO modulador de amplitude em 0.5Hz
                float lfo = 0.75f + 0.25f * MathF.Sin(2f * MathF.PI * 0.5f * t);

                buffer[i] = (noise[i] * lfo) + subBeat;
            }

            // Fade de borda contínuo para evitar clicks nas emendas de loop
            int crossfadeSamples = (int)(SampleRate * 0.020f);
            for (int i = 0; i < crossfadeSamples; i++)
            {
                float weight = i / (float)crossfadeSamples;
                buffer[i] *= weight;
                buffer[count - 1 - i] *= weight;
            }

            ApplyTanhMastering(buffer, drive: 1.3f, masterGain: 0.70f);
            return buffer;
        }

        /// <summary>
        /// 13. ContinuousDamagePulse: Pulso periódico curto de vibração estrutural (55Hz, LPF 150Hz, 40ms).
        /// </summary>
        private static float[] GenerateContinuousDamagePulse()
        {
            int count = (int)(SampleRate * 0.040f); // 40ms
            float[] buffer = new float[count];
            var rng = new Random(1212);

            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.40f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 150f, q: 0.707f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float sub = MathF.Sin(2f * MathF.PI * 55f * t) * MathF.Exp(-t / 0.02f) * 0.65f;
                buffer[i] = noise[i] + sub;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.025f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.2f, masterGain: 0.75f);
            return buffer;
        }

        /// <summary>
        /// 14. EmpBlast: Pulso eletromagnético ressonante de média-baixa frequência (180Hz -> 60Hz com distorção).
        /// </summary>
        private static float[] GenerateEmpBlast()
        {
            int count = (int)(SampleRate * 0.300f); // 300ms
            float[] buffer = new float[count];
            var rng = new Random(1515);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.60f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 350f, endCutoff: 60f, q: 1.5f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float freq = MathF.Max(60f, 180f - (t / 0.20f) * 120f);
                float tone = MathF.Sin(2f * MathF.PI * freq * t) * MathF.Exp(-t / 0.12f) * 0.75f;
                buffer[i] = noise[i] + tone;
            }

            ApplyEnvelope(buffer, attackSec: 0.003f, decaySec: 0.180f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.6f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// 15. WaveAlert: Drone pulsante abafado de alerta (140Hz e 150Hz com batimento lento).
        /// </summary>
        private static float[] GenerateWaveAlert()
        {
            int count = (int)(SampleRate * 0.240f); // 240ms
            float[] buffer = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float tone1 = MathF.Sin(2f * MathF.PI * 140f * t);
                float tone2 = MathF.Sin(2f * MathF.PI * 150f * t);
                buffer[i] = (tone1 + tone2) * 0.45f * MathF.Exp(-t / 0.15f);
            }

            ApplyEnvelope(buffer, attackSec: 0.005f, decaySec: 0.160f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.1f, masterGain: 0.80f);
            return buffer;
        }

        /// <summary>
        /// 16. RockHit: Estalo percussivo mineral nítido (BPF 850Hz) com corpo rochoso denso (240Hz -> 95Hz em 35ms).
        /// </summary>
        private static float[] GenerateRockHit()
        {
            int count = (int)(SampleRate * 0.050f); // 50ms
            float[] buffer = new float[count];
            var rng = new Random(1616);

            // 1. Transiente mineral
            float[] snapNoise = new float[count];
            for (int i = 0; i < count; i++)
            {
                snapNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            ApplyBiquadBpf(snapNoise, SampleRate, centerFreq: 850f, q: 1.5f);
            ApplyEnvelope(snapNoise, attackSec: 0.0005f, decaySec: 0.012f, SampleRate);

            // 2. Ruído e corpo rochoso
            float[] noise = new float[count];
            GenerateBrownNoise(noise, rng, 0.55f);
            ApplyBiquadLpf(noise, SampleRate, cutoff: 280f, q: 0.8f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float rockFreq = MathF.Max(95f, 240f - (t / 0.035f) * 145f);
                float rockTone = MathF.Sin(2f * MathF.PI * rockFreq * t) * MathF.Exp(-t / 0.025f) * 0.70f;
                buffer[i] = (snapNoise[i] * 0.80f) + (noise[i] * 0.35f) + rockTone;
            }

            ApplyEnvelope(buffer, attackSec: 0.001f, decaySec: 0.035f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.5f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// 17. AcidSizzle: Efervescência química abafada (ruído rosa com BPF dinâmico em 450Hz).
        /// </summary>
        private static float[] GenerateAcidSizzle()
        {
            int count = (int)(SampleRate * 0.100f); // 100ms
            float[] buffer = new float[count];
            var rng = new Random(1717);

            GeneratePinkNoise(buffer, rng, 0.70f);
            ApplyBiquadBpf(buffer, SampleRate, centerFreq: 450f, q: 3.0f);
            ApplyEnvelope(buffer, attackSec: 0.004f, decaySec: 0.060f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.2f, masterGain: 0.75f);
            return buffer;
        }

        /// <summary>
        /// 18. BossChargeUp: Rampa de pitch ascendente FM (120Hz -> 680Hz) com ressonância de carregamento de energia
        /// e sub-grave expansivo masterizado com saturação suave Tanh.
        /// </summary>
        private static float[] GenerateBossChargeUp()
        {
            float durationSec = 0.380f; // 380ms
            int count = (int)(SampleRate * durationSec);
            float[] buffer = new float[count];
            var rng = new Random(1818);

            float[] noise = new float[count];
            GeneratePinkNoise(noise, rng, 0.50f);
            ApplyTimeVaryingLpf(noise, SampleRate, startCutoff: 300f, endCutoff: 1400f, q: 2.2f);

            float carrierPhase = 0f;
            float modPhase = 0f;
            float subPhase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / durationSec;

                // Frequência portadora com rampa ascendente de 120Hz para 680Hz
                float carrierFreq = 120f + progress * 560f;
                carrierPhase += MathHelper.TwoPi * carrierFreq / SampleRate;

                // Modulador FM com índice crescente (1.5 -> 5.0) para enriquecer harmônicos
                float modFreq = 95f + progress * 80f;
                modPhase += MathHelper.TwoPi * modFreq / SampleRate;
                float modIndex = 1.5f + progress * 3.5f;

                float fmSignal = MathF.Sin(carrierPhase + modIndex * MathF.Sin(modPhase));

                // Sub-grave crescente de sustentação (60Hz -> 140Hz)
                float subFreq = 60f + progress * 80f;
                subPhase += MathHelper.TwoPi * subFreq / SampleRate;
                float subSine = MathF.Sin(subPhase) * 0.45f;

                // Crescendo exponencial
                float swell = 0.20f + 0.80f * MathF.Pow(progress, 1.4f);

                buffer[i] = (fmSignal * 0.65f + subSine + noise[i] * 0.40f) * swell;
            }

            ApplyEnvelope(buffer, attackSec: 0.320f, decaySec: 0.055f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.55f, masterGain: 0.90f);
            return buffer;
        }

        /// <summary>
        /// 19. BossAttackTelegraph: Aviso sonoro de mira iminente estilo Cuphead / Dead Cells.
        /// Chime harmônico dual-tone (880Hz + 1320Hz) com estalo rápido percussivo e cauda ressonante cristalina.
        /// </summary>
        private static float[] GenerateBossAttackTelegraph()
        {
            float durationSec = 0.180f; // 180ms
            int count = (int)(SampleRate * durationSec);
            float[] buffer = new float[count];
            var rng = new Random(1919);

            float[] snapNoise = new float[count];
            for (int i = 0; i < count; i++) snapNoise[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            ApplyBiquadBpf(snapNoise, SampleRate, centerFreq: 2400f, q: 4.0f);
            ApplyEnvelope(snapNoise, attackSec: 0.0005f, decaySec: 0.010f, SampleRate);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Mordida inicial rápida de pitch (primeiros 20ms)
                float bite = t < 0.020f ? (1.0f - (t / 0.020f)) * 320f : 0f;

                // Partiais harmônicas límpidas A5 (880Hz) e E6 (1320Hz)
                float f1 = 880f + bite;
                float f2 = 1320f + bite * 1.2f;

                float tone1 = MathF.Sin(MathHelper.TwoPi * f1 * t);
                float tone2 = MathF.Sin(MathHelper.TwoPi * f2 * t) * 0.65f;
                float overtone = MathF.Sin(MathHelper.TwoPi * (f1 * 2.5f) * t) * 0.20f * MathF.Exp(-t / 0.035f);

                buffer[i] = (tone1 * 0.55f + tone2 * 0.35f + overtone + snapNoise[i] * 0.25f);
            }

            ApplyEnvelope(buffer, attackSec: 0.0015f, decaySec: 0.090f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.4f, masterGain: 0.88f);
            return buffer;
        }

        /// <summary>
        /// 20. StatusSlowApplied: Efeito sonoro de congelamento criogênico / desaceleração temporal.
        /// Estilhaço de cristais de gelo, varredura passa-faixa descendente e sub-thump inercial.
        /// </summary>
        private static float[] GenerateStatusSlowApplied()
        {
            float durationSec = 0.220f; // 220ms
            int count = (int)(SampleRate * durationSec);
            float[] buffer = new float[count];
            var rng = new Random(2020);

            float[] frostNoise = new float[count];
            GeneratePinkNoise(frostNoise, rng, 0.75f);
            ApplyTimeVaryingLpf(frostNoise, SampleRate, startCutoff: 2200f, endCutoff: 450f, q: 2.0f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // Sub-drag inercial descendente (160Hz -> 45Hz)
                float subFreq = MathF.Max(45f, 160f - (t / 0.080f) * 115f);
                float sub = MathF.Sin(MathHelper.TwoPi * subFreq * t) * MathF.Exp(-t / 0.060f) * 0.70f;

                // Cristais de gelo estalando (1760Hz e 2640Hz)
                float crystal = (MathF.Sin(MathHelper.TwoPi * 1760f * t) * 0.40f +
                                 MathF.Sin(MathHelper.TwoPi * 2640f * t) * 0.25f) * MathF.Exp(-t / 0.035f);

                buffer[i] = frostNoise[i] * 0.55f + sub + crystal;
            }

            ApplyEnvelope(buffer, attackSec: 0.002f, decaySec: 0.120f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.35f, masterGain: 0.86f);
            return buffer;
        }

        /// <summary>
        /// 21. StatusPoisonApplied: Efervescência ácida tóxica e borbulhar cáustico.
        /// Ruído rosa com modulação sinusoidal LFO rápida somado a modulação de frequência squelch.
        /// </summary>
        private static float[] GenerateStatusPoisonApplied()
        {
            float durationSec = 0.240f; // 240ms
            int count = (int)(SampleRate * durationSec);
            float[] buffer = new float[count];
            var rng = new Random(2121);

            float[] acidNoise = new float[count];
            GeneratePinkNoise(acidNoise, rng, 0.80f);
            ApplyBiquadBpf(acidNoise, SampleRate, centerFreq: 850f, q: 2.5f);

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                // LFO de borbulhar (32Hz)
                float bubbleLfo = 0.5f + 0.5f * MathF.Sin(MathHelper.TwoPi * 32f * t);

                // Modulação squelch FM viscosa (110Hz modulada a 40Hz)
                float fm = MathF.Sin(MathHelper.TwoPi * 110f * t + 2.5f * MathF.Sin(MathHelper.TwoPi * 40f * t)) * MathF.Exp(-t / 0.080f) * 0.55f;

                buffer[i] = (acidNoise[i] * bubbleLfo * 0.70f + fm);
            }

            ApplyEnvelope(buffer, attackSec: 0.003f, decaySec: 0.140f, SampleRate);
            ApplyTanhMastering(buffer, drive: 1.4f, masterGain: 0.86f);
            return buffer;
        }

        #endregion

        #region Primitivas DSP e Filtros Digitais IIR

        /// <summary>
        /// Gera ruído rosa (Pink Noise 1/f) utilizando o algoritmo de Paul Kellett de 7 polos.
        /// </summary>
        public static void GeneratePinkNoise(float[] buffer, Random rng, float gain)
        {
            float b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);

                b0 = 0.99886f * b0 + white * 0.0555179f;
                b1 = 0.99332f * b1 + white * 0.0750759f;
                b2 = 0.96900f * b2 + white * 0.1538520f;
                b3 = 0.86650f * b3 + white * 0.3104856f;
                b4 = 0.55000f * b4 + white * 0.5329522f;
                b5 = -0.7616f * b5 - white * 0.0168980f;

                float pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f;
                b6 = white * 0.115926f;

                buffer[i] = (pink * 0.12f) * gain;
            }
        }

        /// <summary>
        /// Gera ruído marrom (Brown / Brownian Noise) através da integração com vazamento do ruído branco.
        /// Produz uma densidade acústica quente e aveludada sem agudos cortantes.
        /// </summary>
        public static void GenerateBrownNoise(float[] buffer, Random rng, float gain)
        {
            float lastOutput = 0.0f;

            for (int i = 0; i < buffer.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lastOutput = (lastOutput + (0.05f * white)) / 1.045f;
                buffer[i] = (lastOutput * 3.2f) * gain;
            }
        }

        /// <summary>
        /// Aplica um filtro passa-baixa IIR de 2ª ordem (Biquad Low-Pass Filter) com corte e ressonância definidos.
        /// </summary>
        public static void ApplyBiquadLpf(float[] buffer, float sampleRate, float cutoff, float q = 0.707f)
        {
            if (cutoff <= 10f) cutoff = 10f;
            if (cutoff >= sampleRate * 0.49f) cutoff = sampleRate * 0.49f;

            float w0 = 2f * MathF.PI * cutoff / sampleRate;
            float cosW0 = MathF.Cos(w0);
            float sinW0 = MathF.Sin(w0);
            float alpha = sinW0 / (2f * q);

            float b0 = (1f - cosW0) * 0.5f;
            float b1 = 1f - cosW0;
            float b2 = (1f - cosW0) * 0.5f;
            float a0 = 1f + alpha;
            float a1 = -2f * cosW0;
            float a2 = 1f - alpha;

            float invA0 = 1f / a0;
            b0 *= invA0;
            b1 *= invA0;
            b2 *= invA0;
            a1 *= invA0;
            a2 *= invA0;

            float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float x0 = buffer[i];
                float y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

                x2 = x1;
                x1 = x0;
                y2 = y1;
                y1 = y0;

                buffer[i] = y0;
            }
        }

        /// <summary>
        /// Aplica um filtro passa-alta IIR de 2ª ordem (Biquad High-Pass Filter) para isolar transientes e estalos agudos.
        /// </summary>
        public static void ApplyBiquadHpf(float[] buffer, float sampleRate, float cutoff, float q = 0.707f)
        {
            if (cutoff <= 10f) cutoff = 10f;
            if (cutoff >= sampleRate * 0.49f) cutoff = sampleRate * 0.49f;

            float w0 = 2f * MathF.PI * cutoff / sampleRate;
            float cosW0 = MathF.Cos(w0);
            float sinW0 = MathF.Sin(w0);
            float alpha = sinW0 / (2f * q);

            float b0 = (1f + cosW0) * 0.5f;
            float b1 = -(1f + cosW0);
            float b2 = (1f + cosW0) * 0.5f;
            float a0 = 1f + alpha;
            float a1 = -2f * cosW0;
            float a2 = 1f - alpha;

            float invA0 = 1f / a0;
            b0 *= invA0;
            b1 *= invA0;
            b2 *= invA0;
            a1 *= invA0;
            a2 *= invA0;

            float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float x0 = buffer[i];
                float y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

                x2 = x1;
                x1 = x0;
                y2 = y1;
                y1 = y0;

                buffer[i] = y0;
            }
        }

        /// <summary>
        /// Aplica um filtro passa-faixa IIR de 2ª ordem (Biquad Band-Pass Filter) de ganho unitário no pico.
        /// </summary>
        public static void ApplyBiquadBpf(float[] buffer, float sampleRate, float centerFreq, float q = 2.0f)
        {
            if (centerFreq <= 10f) centerFreq = 10f;
            if (centerFreq >= sampleRate * 0.49f) centerFreq = sampleRate * 0.49f;

            float w0 = 2f * MathF.PI * centerFreq / sampleRate;
            float sinW0 = MathF.Sin(w0);
            float cosW0 = MathF.Cos(w0);
            float alpha = sinW0 / (2f * q);

            float b0 = alpha;
            float b1 = 0f;
            float b2 = -alpha;
            float a0 = 1f + alpha;
            float a1 = -2f * cosW0;
            float a2 = 1f - alpha;

            float invA0 = 1f / a0;
            b0 *= invA0;
            b1 *= invA0;
            b2 *= invA0;
            a1 *= invA0;
            a2 *= invA0;

            float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float x0 = buffer[i];
                float y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

                x2 = x1;
                x1 = x0;
                y2 = y1;
                y1 = y0;

                buffer[i] = y0;
            }
        }

        /// <summary>
        /// Aplica filtro passa-baixa IIR dinâmico com frequência de corte variável no tempo (Sweep LPF).
        /// </summary>
        public static void ApplyTimeVaryingLpf(float[] buffer, float sampleRate, float startCutoff, float endCutoff, float q = 0.707f)
        {
            float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            int length = buffer.Length;

            for (int i = 0; i < length; i++)
            {
                float progress = i / (float)length;
                float cutoff = MathHelper.Lerp(startCutoff, endCutoff, progress);
                if (cutoff <= 10f) cutoff = 10f;
                if (cutoff >= sampleRate * 0.49f) cutoff = sampleRate * 0.49f;

                float w0 = 2f * MathF.PI * cutoff / sampleRate;
                float cosW0 = MathF.Cos(w0);
                float sinW0 = MathF.Sin(w0);
                float alpha = sinW0 / (2f * q);

                float b0 = (1f - cosW0) * 0.5f;
                float b1 = 1f - cosW0;
                float b2 = (1f - cosW0) * 0.5f;
                float a0 = 1f + alpha;
                float a1 = -2f * cosW0;
                float a2 = 1f - alpha;

                float invA0 = 1f / a0;
                b0 *= invA0;
                b1 *= invA0;
                b2 *= invA0;
                a1 *= invA0;
                a2 *= invA0;

                float x0 = buffer[i];
                float y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;

                x2 = x1;
                x1 = x0;
                y2 = y1;
                y1 = y0;

                buffer[i] = y0;
            }
        }

        /// <summary>
        /// Aplica envelope ADSR suave com ataque anti-clique (fade-in linear),
        /// decaimento exponencial natural e zeramento estrito nos 5% finais da cauda.
        /// </summary>
        public static void ApplyEnvelope(float[] buffer, float attackSec, float decaySec, float sampleRate, bool strictTailZero = true)
        {
            int totalSamples = buffer.Length;
            int attackSamples = Math.Max(1, (int)(attackSec * sampleRate));
            int tailFadeSamples = (int)(totalSamples * 0.05f);

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / sampleRate;
                float env = 1.0f;

                // 1. Attack Ramp (Linear Anti-Click)
                if (i < attackSamples)
                {
                    env = i / (float)attackSamples;
                }
                else
                {
                    // 2. Exponential Decay
                    float decayTime = t - attackSec;
                    env = MathF.Exp(-decayTime / decaySec);
                }

                // 3. Strict 5% Tail Fade-Out to prevent DC pops
                if (strictTailZero && i >= totalSamples - tailFadeSamples)
                {
                    int tailIndex = i - (totalSamples - tailFadeSamples);
                    float tailRamp = 1.0f - (tailIndex / (float)tailFadeSamples);
                    env *= MathF.Max(0f, tailRamp);
                }

                buffer[i] *= env;
            }

            if (strictTailZero && totalSamples > 0)
            {
                buffer[totalSamples - 1] = 0f;
            }
        }

        /// <summary>
        /// Saturação suave não-linear analógica via Tanh e ganho final calibrado.
        /// </summary>
        public static void ApplyTanhMastering(float[] buffer, float drive, float masterGain = 0.85f)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = MathF.Tanh(buffer[i] * drive) * masterGain;
            }
        }

        /// <summary>
        /// Converte amostras float [-1.0, 1.0] em PCM 16-bit assinado little-endian.
        /// </summary>
        public static byte[] ConvertFloatsToPCM16(float[] samples)
        {
            byte[] pcm = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = MathHelper.Clamp(samples[i], -1.0f, 1.0f);
                short pcm16 = (short)(clamped * 32767.0f);

                pcm[i * 2] = (byte)(pcm16 & 0xFF);
                pcm[i * 2 + 1] = (byte)((pcm16 >> 8) & 0xFF);
            }

            return pcm;
        }

        #endregion
    }
}
