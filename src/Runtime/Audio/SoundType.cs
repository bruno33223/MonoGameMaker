namespace MonoGameMaker.Runtime.Audio
{
    /// <summary>
    /// Escala dimensional de detonação para o sistema de síntese multi-banda e acúmulo de explosões.
    /// </summary>
    public enum ExplosionScale
    {
        Small,
        Medium,
        Heavy
    }

    /// <summary>
    /// Categorias semânticas para controle de concorrência e limitação de vozes (Voice Limiter).
    /// </summary>
    public enum SoundCategory
    {
        PlayerWeapons,
        EnemyWeapons,
        Souls,
        Impacts,
        Explosions,
        Loops,
        UIAndAlerts
    }

    /// <summary>
    /// Tipos discriminados de áudio para o subsistema de síntese procedural e gerenciamento de canais.
    /// Segrega explicitamente assinaturas sonoras de armas do jogador, arquétipos de inimigos, chefes, impactos, núcleos, explosões e UI.
    /// </summary>
    public enum SoundType
    {
        // Player Weapons & Movement (Pneumatic Breath & Sub-Thumps)
        PlayerShotLight,
        PlayerShotHeavy,
        PlayerChargeUp,
        PlayerChargeRelease,
        DashWhoosh,
        SpecialNova,
        PowerupCollect,
        SoulPickup,

        // Enemy & Boss Audio Signatures (Muffled Low-Pass Plasma)
        EnemyShotPlasma,
        EnemyShotHeavy,
        BossEnergyBlast,
        BossChargeUp,
        BossAttackTelegraph,
        StatusSlowApplied,
        StatusPoisonApplied,

        // Faction-Specific Enemy Audio Signatures (Procedural DSP)
        // Toxis (Bio-acid organic / chimeric)
        ToxisSpitBioAcid,
        ToxisChargeWarn,
        ToxisDeathExplosion,

        // Kyros (Resonant crystal / cryogenic)
        KyrosCrystalResonance,
        KyrosChargeWarn,
        KyrosShatter,

        // Titans & Caldera (Heavy industrial sub-bass / volcanic forge)
        TitanIndustrialBass,
        TitanChargeWarn,
        TitanDestructionSub,

        // Chronos & Umbra (Tachyon phase glitch / void singularities)
        ChronosTachyonGlitch,
        ChronosPhaseWarn,
        ChronosWarpCollapse,

        // Explosions (Multi-Band Layered & Accretion Stacking)
        ExplosionSmall,
        ExplosionMedium,
        ExplosionHeavy,
        ExplosionMegaChain,

        // UI & Holographic Crystal Menu Feedback (Crystal Sine Harmonics)
        UiHover,
        UiSelect,
        UiBack,
        UiUpgradePurchased,

        // Impacts & Structural Hull Collisions
        ImpactShield,
        ImpactHull,
        CriticalHitPunch,

        // Sustained Loops & Continuous Pulsing Hazards
        BlackHoleDrone,
        ContinuousDamagePulse,

        // Auxiliary & Environmental Events
        EmpBlast,
        WaveAlert,
        RockHit,
        AcidSizzle,
        RamArtifactDrop,

        // Retrocompatibility Aliases
        PlayerLaser = PlayerShotLight,
        PlayerHeavyLaser = PlayerShotHeavy,
        PlayerExplosion = ExplosionHeavy,
        Dash = DashWhoosh,
        EnemyLaserLight = EnemyShotPlasma,
        EnemyLaserHeavy = EnemyShotHeavy,
        EnemyExplosionSmall = ExplosionSmall,
        EnemyExplosionLarge = ExplosionHeavy,
        ExplosionCosmic = ExplosionHeavy,
        BlackHoleLoop = BlackHoleDrone,
        ImpactArmor = ImpactHull,

        // Faction Audio Survey Aliases
        EnemyToxisBioSpit = ToxisSpitBioAcid,
        EnemyToxisAcidCharge = ToxisChargeWarn,
        EnemyToxisSplatDestroy = ToxisDeathExplosion,
        EnemyKyrosCrystalShot = KyrosCrystalResonance,
        EnemyKyrosPrismCharge = KyrosChargeWarn,
        EnemyKyrosShatterDestroy = KyrosShatter,
        EnemyCalderaHeavyCannon = TitanIndustrialBass,
        EnemyCalderaForgeCharge = TitanChargeWarn,
        EnemyCalderaSeismicImpact = TitanDestructionSub,
        EnemyChronosTachyonBeam = ChronosTachyonGlitch,
        EnemyChronosWarpCharge = ChronosPhaseWarn,
        EnemyChronosGlitchRift = ChronosWarpCollapse
    }
}
