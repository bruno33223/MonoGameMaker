using System;

namespace TestConsole.Tests.Domain
{
    public enum DamageType
    {
        Physical,
        Plasma,
        Fire,
        Poison,
        Ice
    }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public float ArmorPenetration;
        public bool IsCritical;
        public float CritMultiplier;

        public static DamageInfo FromRaw(
            float amount,
            DamageType type = DamageType.Physical,
            float armorPenetration = 0f,
            bool isCrit = false,
            float critMultiplier = 2.0f)
        {
            return new DamageInfo
            {
                Amount = amount,
                Type = type,
                ArmorPenetration = armorPenetration,
                IsCritical = isCrit,
                CritMultiplier = critMultiplier
            };
        }
    }

    public class StatProfile
    {
        public float MaxHull { get; set; } = 100f;
        public float MaxShield { get; set; } = 50f;
        public float Armor { get; set; } = 0f;
        public float FireResistance { get; set; } = 0f;
        public float PoisonResistance { get; set; } = 0f;
        public float IceResistance { get; set; } = 0f;
        public float CritResistance { get; set; } = 0f;

        public StatProfile() { }

        public StatProfile(float maxHull, float maxShield, float armor = 0f)
        {
            MaxHull = maxHull;
            MaxShield = maxShield;
            Armor = armor;
        }
    }

    public class HealthController
    {
        private float _hullHealth;
        private float _shieldHealth;

        public StatProfile Stats { get; set; }
        public float HullHealth => _hullHealth;
        public float ShieldHealth => _shieldHealth;
        public bool IsDead => _hullHealth <= 0f;

        public event Action? OnShieldBroken;

        public HealthController(StatProfile stats)
        {
            Stats = stats ?? new StatProfile();
            _hullHealth = Stats.MaxHull;
            _shieldHealth = Stats.MaxShield;
        }

        public HealthController(float maxHull, float maxShield, float armor = 0f)
            : this(new StatProfile(maxHull, maxShield, armor))
        {
        }

        public static float CalculateMitigatedDamage(DamageInfo info, StatProfile stats)
        {
            if (stats == null || info.Amount <= 0f) return 0f;

            float rawAmount = info.Amount;

            // 1. Dano crítico com mitigação por CritResistance
            if (info.IsCritical)
            {
                float critBonus = Math.Max(0f, info.CritMultiplier - 1f);
                float effectiveCritMult = 1f + (critBonus * (1f - Math.Clamp(stats.CritResistance, 0f, 0.95f)));
                rawAmount *= effectiveCritMult;
            }

            // 2. Mitigação por tipo de dano e blindagem
            switch (info.Type)
            {
                case DamageType.Physical:
                {
                    float effectiveArmor = stats.Armor * (1f - Math.Clamp(info.ArmorPenetration, 0f, 1f));
                    float armorFactor = 100f / (100f + Math.Max(0f, effectiveArmor));
                    return rawAmount * armorFactor;
                }

                case DamageType.Plasma:
                {
                    float effectivePen = Math.Max(info.ArmorPenetration, 0.5f);
                    float effectiveArmor = stats.Armor * (1f - Math.Clamp(effectivePen, 0f, 1f));
                    float armorFactor = 100f / (100f + Math.Max(0f, effectiveArmor));
                    return rawAmount * armorFactor;
                }

                case DamageType.Fire:
                {
                    float res = Math.Clamp(stats.FireResistance, 0f, 0.95f);
                    return rawAmount * (1f - res);
                }

                case DamageType.Poison:
                {
                    float res = Math.Clamp(stats.PoisonResistance, 0f, 0.95f);
                    return rawAmount * (1f - res);
                }

                case DamageType.Ice:
                {
                    float res = Math.Clamp(stats.IceResistance, 0f, 0.95f);
                    return rawAmount * (1f - res);
                }

                default:
                    return rawAmount;
            }
        }

        public bool TakeDamage(DamageInfo info, out float actualDamageTaken, out bool shieldAbsorbed, Action? onShieldBrokenCallback = null)
        {
            if (IsDead)
            {
                actualDamageTaken = 0f;
                shieldAbsorbed = false;
                return true;
            }

            float mitigated = CalculateMitigatedDamage(info, Stats);
            actualDamageTaken = mitigated;

            if (_shieldHealth > 0f)
            {
                if (_shieldHealth > mitigated)
                {
                    _shieldHealth -= mitigated;
                    shieldAbsorbed = true;
                    return IsDead;
                }
                else
                {
                    float leftover = mitigated - _shieldHealth;
                    _shieldHealth = 0f;
                    onShieldBrokenCallback?.Invoke();
                    OnShieldBroken?.Invoke();
                    _hullHealth = Math.Max(0f, _hullHealth - leftover);
                    shieldAbsorbed = leftover <= 0f;
                    return IsDead;
                }
            }

            _hullHealth = Math.Max(0f, _hullHealth - mitigated);
            shieldAbsorbed = false;
            return IsDead;
        }

        public bool TakeDamage(DamageInfo info) => TakeDamage(info, out _, out _);

        public void ResetToMax()
        {
            _hullHealth = Stats.MaxHull;
            _shieldHealth = Stats.MaxShield;
        }
    }
}
