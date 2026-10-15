using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MonoGameMaker.Runtime.Audio
{
    /// <summary>
    /// Gerenciador centralizado de áudio com suporte a Throttling por Timestamp,
    /// Voice Stealing por Categoria, Mecanismo de Acúmulo de Detonações (Explosion Accretion System),
    /// Bypass de latência zero para UI, Gestão de Loops Sustentados e tolerância headless.
    /// </summary>
    public class AudioManager
    {
        private static AudioManager? _instance;
        public static AudioManager Instance => _instance ??= new AudioManager();

        private Dictionary<SoundType, SoundEffect> _effectCache = new();
        private readonly Dictionary<SoundType, double> _lastPlayedTime = new();
        private readonly Dictionary<SoundCategory, List<SoundEffectInstance>> _activeVoices = new();
        private readonly Dictionary<object, SoundEffectInstance?> _activeLoops = new();
        private readonly Dictionary<object, double> _loopLastSeen = new();

        private readonly Random _rng = new();
        private double _currentTimeSeconds = 0.0;
        private bool _useManualTimeForTest = false;

        public float MasterVolume { get; set; } = 1.0f;
        public float SfxVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 1.0f;

        // Controle de lote e modulação de pitch para coletas massivas de núcleos
        private int _soulBatchCount = 0;
        private double _lastSoulPickupTime = -1.0;

        // Mecanismo de Acúmulo de Detonações (Explosion Accretion Tracker)
        private int _recentExplosionCount = 0;
        private double _lastExplosionTime = -1.0;
        public const double AccretionWindow = 0.120; // 120ms
        public const double LoopHeartbeatTimeout = 0.150; // 150ms tolerância sem heartbeat antes de descartar loop

        // Capacidade máxima de vozes simultâneas por categoria
        private static readonly Dictionary<SoundCategory, int> CategoryVoiceLimits = new()
        {
            { SoundCategory.PlayerWeapons, 3 },
            { SoundCategory.EnemyWeapons,  3 },
            { SoundCategory.Souls,         2 },
            { SoundCategory.Impacts,       4 },
            { SoundCategory.Explosions,    3 },
            { SoundCategory.Loops,         4 },
            { SoundCategory.UIAndAlerts,   4 }
        };

        // Janelas mínimas de debounce (em segundos)
        private static readonly Dictionary<SoundType, double> ThrottlingIntervals = new()
        {
            { SoundType.SoulPickup,            0.030 },
            { SoundType.ImpactShield,          0.025 },
            { SoundType.ImpactHull,            0.020 },
            { SoundType.ContinuousDamagePulse, 0.150 },
            { SoundType.PlayerShotLight,       0.025 },
            { SoundType.PlayerShotHeavy,       0.035 },
            { SoundType.EnemyShotPlasma,       0.030 },
            { SoundType.EnemyShotHeavy,        0.040 },
            { SoundType.RockHit,               0.025 },
            { SoundType.AcidSizzle,            0.050 },
            { SoundType.BossChargeUp,          0.050 },
            { SoundType.BossAttackTelegraph,   0.040 },
            { SoundType.StatusSlowApplied,     0.050 },
            { SoundType.StatusPoisonApplied,   0.050 },
            { SoundType.ToxisSpitBioAcid,      0.030 },
            { SoundType.ToxisChargeWarn,       0.050 },
            { SoundType.ToxisDeathExplosion,   0.040 },
            { SoundType.KyrosCrystalResonance, 0.025 },
            { SoundType.KyrosChargeWarn,       0.050 },
            { SoundType.KyrosShatter,          0.040 },
            { SoundType.TitanIndustrialBass,   0.035 },
            { SoundType.TitanChargeWarn,       0.050 },
            { SoundType.TitanDestructionSub,   0.050 },
            { SoundType.ChronosTachyonGlitch,  0.030 },
            { SoundType.ChronosPhaseWarn,      0.050 },
            { SoundType.ChronosWarpCollapse,   0.040 }
        };

        public AudioManager()
        {
            foreach (SoundCategory cat in Enum.GetValues(typeof(SoundCategory)))
            {
                _activeVoices[cat] = new List<SoundEffectInstance>();
            }
        }

        public void InitializeCache(Dictionary<SoundType, SoundEffect> cache)
        {
            _effectCache = cache;
        }

        /// <summary>
        /// Atualiza o relógio de áudio, limpa vozes finalizadas e purga loops órfãos sem heartbeat.
        /// </summary>
        public void Update(float dt, double totalGameTimeSeconds = 0)
        {
            if (!_useManualTimeForTest)
            {
                _currentTimeSeconds = totalGameTimeSeconds > 0 ? totalGameTimeSeconds : _currentTimeSeconds + dt;
            }

            // Reseta contador de lote de núcleos se passar da janela de debounce
            if (_currentTimeSeconds - _lastSoulPickupTime > 0.060)
            {
                _soulBatchCount = 0;
            }

            // Reseta acúmulo de explosões se exceder a janela de 120ms
            if (_currentTimeSeconds - _lastExplosionTime > AccretionWindow)
            {
                _recentExplosionCount = 0;
            }

            // Watchdog de loops órfãos
            List<object>? deadLoops = null;
            foreach (var kvp in _activeLoops)
            {
                if (_loopLastSeen.TryGetValue(kvp.Key, out double lastSeen))
                {
                    if (_currentTimeSeconds - lastSeen > LoopHeartbeatTimeout)
                    {
                        deadLoops ??= new List<object>();
                        deadLoops.Add(kvp.Key);
                    }
                }
            }

            if (deadLoops != null)
            {
                for (int i = 0; i < deadLoops.Count; i++)
                {
                    StopLoop(deadLoops[i]);
                }
            }

            // Limpa vozes concluídas
            foreach (var kvp in _activeVoices)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var voice = list[i];
                    if (voice.IsDisposed || voice.State == SoundState.Stopped)
                    {
                        if (!voice.IsDisposed)
                        {
                            try { voice.Dispose(); } catch { }
                        }
                        list.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Despacha uma explosão através do Mecanismo de Acúmulo de Detonações (Explosion Accretion System).
        /// Em janelas de 120ms, explosões concorrentes ganham peso logarítmico e detuning de pitch descendente.
        /// Ao atingir 4+ detonações, comuta automaticamente para ExplosionMegaChain.
        /// </summary>
        public bool PlayExplosion(ExplosionScale scale = ExplosionScale.Medium, float volume = 1f, float pitch = 0f)
        {
            if (_currentTimeSeconds - _lastExplosionTime <= AccretionWindow && _lastExplosionTime >= 0.0)
            {
                _recentExplosionCount++;
            }
            else
            {
                _recentExplosionCount = 1;
            }
            _lastExplosionTime = _currentTimeSeconds;

            if (_recentExplosionCount >= 4)
            {
                return Play(SoundType.ExplosionMegaChain, volume: 1.0f, pitch: -0.08f);
            }
            else
            {
                float volMod = Math.Min(1.0f, volume * (1.0f + 0.25f * MathF.Log2(_recentExplosionCount)));
                float pitchMod = MathHelper.Clamp(pitch - 0.04f * (_recentExplosionCount - 1), -1.0f, 1.0f);

                SoundType explosionType = scale switch
                {
                    ExplosionScale.Small => SoundType.ExplosionSmall,
                    ExplosionScale.Medium => SoundType.ExplosionMedium,
                    ExplosionScale.Heavy => SoundType.ExplosionHeavy,
                    _ => SoundType.ExplosionMedium
                };

                return Play(explosionType, volMod, pitchMod);
            }
        }

        /// <summary>
        /// Toca um som de disparo único (one-shot) com throttling por timestamp,
        /// voice stealing, tolerância headless e micro-jitter de pitch e volume.
        /// </summary>
        public bool Play(SoundType type, float volume = 1f, float pitch = 0f)
        {
            SoundCategory category = GetCategory(type);

            // 1. Throttling & Debounce por Timestamp (Bypass total para UI e Sons Críticos)
            if (category != SoundCategory.UIAndAlerts && ThrottlingIntervals.TryGetValue(type, out double minInterval))
            {
                if (_lastPlayedTime.TryGetValue(type, out double lastTime))
                {
                    if (_currentTimeSeconds - lastTime < minInterval)
                    {
                        if (type == SoundType.SoulPickup)
                        {
                            _soulBatchCount++;
                        }
                        return false;
                    }
                }
            }

            _lastPlayedTime[type] = _currentTimeSeconds;

            if (type == SoundType.SoulPickup)
            {
                _lastSoulPickupTime = _currentTimeSeconds;
                float batchPitchShift = Math.Min(0.15f, _soulBatchCount * 0.02f);
                pitch += batchPitchShift;
                _soulBatchCount = 0;
            }

            if (!_effectCache.TryGetValue(type, out var effect) || effect == null)
            {
                return true;
            }

            // 2. Pitch e Gain Jitter orgânico
            float pitchJitter = category == SoundCategory.UIAndAlerts
                ? 0f
                : (float)(_rng.NextDouble() * 0.12 - 0.06);

            float volumeJitter = category == SoundCategory.UIAndAlerts
                ? 0f
                : (float)(_rng.NextDouble() * 0.10 - 0.05);

            float finalPitch = MathHelper.Clamp(pitch + pitchJitter, -1.0f, 1.0f);
            float finalVolume = MathHelper.Clamp(volume * (1.0f + volumeJitter) * MasterVolume * SfxVolume, 0.0f, 1.0f);

            int maxVoices = CategoryVoiceLimits.TryGetValue(category, out int limit) ? limit : 3;

            try
            {
                var instance = effect.CreateInstance();
                instance.Volume = finalVolume;
                instance.Pitch = finalPitch;
                instance.IsLooped = false;

                // 3. Voice Stealing: remove a voz mais antiga da categoria se lotado
                var voiceList = _activeVoices[category];
                if (voiceList.Count >= maxVoices)
                {
                    var oldest = voiceList[0];
                    if (!oldest.IsDisposed)
                    {
                        try
                        {
                            oldest.Stop();
                            oldest.Dispose();
                        }
                        catch { }
                    }
                    voiceList.RemoveAt(0);
                }

                voiceList.Add(instance);
                instance.Play();
                return true;
            }
            catch
            {
                // Fallback tolerante para ambientes headless sem hardware de áudio
                return true;
            }
        }

        /// <summary>
        /// Inicia ou renova um som sustentado em loop vinculado a uma chave de entidade.
        /// </summary>
        public void PlayLoop(SoundType type, object sourceKey, float volume = 1f)
        {
            if (sourceKey == null) return;

            _loopLastSeen[sourceKey] = _currentTimeSeconds;

            float finalVolume = MathHelper.Clamp(volume * MasterVolume * SfxVolume, 0f, 1f);

            if (_activeLoops.TryGetValue(sourceKey, out var existingInstance))
            {
                if (existingInstance != null && !existingInstance.IsDisposed && existingInstance.State == SoundState.Playing)
                {
                    try { existingInstance.Volume = finalVolume; } catch { }
                    return;
                }
                else if (existingInstance == null)
                {
                    return;
                }
                else
                {
                    try { if (!existingInstance.IsDisposed) existingInstance.Dispose(); } catch { }
                    _activeLoops.Remove(sourceKey);
                }
            }

            _effectCache.TryGetValue(type, out var effect);

            try
            {
                var instance = effect?.CreateInstance();
                if (instance != null)
                {
                    instance.Volume = finalVolume;
                    instance.IsLooped = true;
                    instance.Play();
                    _activeLoops[sourceKey] = instance;
                }
                else
                {
                    _activeLoops[sourceKey] = null;
                }
            }
            catch
            {
                _activeLoops[sourceKey] = null;
            }
        }

        /// <summary>
        /// Encerra imediatamente o loop sustentado associado à entidade e descarta a instância.
        /// </summary>
        public void StopLoop(object sourceKey)
        {
            if (sourceKey == null) return;

            _loopLastSeen.Remove(sourceKey);

            if (_activeLoops.TryGetValue(sourceKey, out var instance))
            {
                try
                {
                    if (instance != null && !instance.IsDisposed)
                    {
                        instance.Stop();
                        instance.Dispose();
                    }
                }
                catch { }
                _activeLoops.Remove(sourceKey);
            }
        }

        /// <summary>
        /// Encerra todos os loops sustentados ativos.
        /// </summary>
        public void StopAllLoops()
        {
            _loopLastSeen.Clear();

            foreach (var kvp in _activeLoops)
            {
                try
                {
                    if (kvp.Value != null && !kvp.Value.IsDisposed)
                    {
                        kvp.Value.Stop();
                        kvp.Value.Dispose();
                    }
                }
                catch { }
            }
            _activeLoops.Clear();
        }

        public bool IsLoopActive(object sourceKey)
        {
            if (sourceKey == null) return false;
            return _activeLoops.ContainsKey(sourceKey);
        }

        public int ActiveLoopCount => _activeLoops.Count;
        public int RecentExplosionCount => _recentExplosionCount;

        /// <summary>
        /// Mapeia cada SoundType à sua categoria semântica.
        /// </summary>
        public static SoundCategory GetCategory(SoundType type)
        {
            return type switch
            {
                SoundType.PlayerShotLight or
                SoundType.PlayerShotHeavy or
                SoundType.DashWhoosh or
                SoundType.SpecialNova or
                SoundType.PowerupCollect
                    => SoundCategory.PlayerWeapons,

                SoundType.EnemyShotPlasma or
                SoundType.EnemyShotHeavy or
                SoundType.BossEnergyBlast or
                SoundType.BossChargeUp or
                SoundType.BossAttackTelegraph or
                SoundType.ToxisSpitBioAcid or
                SoundType.KyrosCrystalResonance or
                SoundType.TitanIndustrialBass or
                SoundType.ChronosTachyonGlitch
                    => SoundCategory.EnemyWeapons,

                SoundType.SoulPickup
                    => SoundCategory.Souls,

                SoundType.ExplosionSmall or
                SoundType.ExplosionMedium or
                SoundType.ExplosionHeavy or
                SoundType.ExplosionMegaChain or
                SoundType.ToxisDeathExplosion or
                SoundType.KyrosShatter or
                SoundType.TitanDestructionSub or
                SoundType.ChronosWarpCollapse
                    => SoundCategory.Explosions,

                SoundType.ImpactShield or
                SoundType.ImpactHull or
                SoundType.RockHit or
                SoundType.StatusSlowApplied or
                SoundType.StatusPoisonApplied
                    => SoundCategory.Impacts,

                SoundType.BlackHoleDrone
                    => SoundCategory.Loops,

                SoundType.ContinuousDamagePulse or
                SoundType.EmpBlast or
                SoundType.WaveAlert or
                SoundType.AcidSizzle or
                SoundType.UiHover or
                SoundType.UiSelect or
                SoundType.UiBack or
                SoundType.UiUpgradePurchased or
                SoundType.RamArtifactDrop or
                SoundType.ToxisChargeWarn or
                SoundType.KyrosChargeWarn or
                SoundType.TitanChargeWarn or
                SoundType.ChronosPhaseWarn
                    => SoundCategory.UIAndAlerts,

                _ => SoundCategory.PlayerWeapons
            };
        }

        #region Helpers de Teste Unitário
        public void SetCurrentTimeForTest(double timeSeconds)
        {
            _useManualTimeForTest = true;
            _currentTimeSeconds = timeSeconds;
        }

        public void ResetForTest()
        {
            _useManualTimeForTest = false;
            _currentTimeSeconds = 0.0;
            _lastPlayedTime.Clear();
            _soulBatchCount = 0;
            _lastSoulPickupTime = -1.0;
            _recentExplosionCount = 0;
            _lastExplosionTime = -1.0;
            StopAllLoops();
            foreach (var list in _activeVoices.Values)
            {
                foreach (var v in list)
                {
                    try { if (!v.IsDisposed) v.Dispose(); } catch { }
                }
                list.Clear();
            }
        }
        #endregion
    }
}
