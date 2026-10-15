using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TestConsole.Tests.Domain
{
    public class EnemyModel
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Radius { get; set; } = 20f;
        public bool IsActive { get; set; } = true;
        public float OffscreenTimer { get; set; } = 0f;

        public static readonly Rectangle ArenaBounds = new Rectangle(-960, -540, 1920, 1080);

        public bool IsOutsideArena =>
            Position.X < ArenaBounds.Left || Position.X > ArenaBounds.Right ||
            Position.Y < ArenaBounds.Top || Position.Y > ArenaBounds.Bottom;

        public void UpdateSteeringAndRecycle(float dt)
        {
            if (IsOutsideArena)
            {
                OffscreenTimer += dt;
                // Aceleração centrípeta orientada para o centro (0, 0)
                Vector2 toCenter = -Position;
                if (toCenter.LengthSquared() > 0.001f)
                {
                    Velocity += Vector2.Normalize(toCenter) * 300f * dt;
                }

                // Reciclagem silenciosa se > 3.5s
                if (OffscreenTimer > 3.5f)
                {
                    IsActive = false;
                }
            }
            else
            {
                OffscreenTimer = 0f;
            }

            Position += Velocity * dt;
        }

        public static void ApplySoftSeparation(EnemyModel a, EnemyModel b, float worldDt)
        {
            Vector2 delta = a.Position - b.Position;
            float dist = delta.Length();
            float desiredDist = a.Radius + b.Radius + 6f;

            if (dist < desiredDist && dist > 0.0001f)
            {
                Vector2 pushDir = delta / dist;
                Vector2 pushForce = pushDir * (desiredDist - dist) * 6.5f * worldDt;
                a.Position += pushForce;
                b.Position -= pushForce;
            }
        }
    }

    public class BossModularPart
    {
        public Vector3 LocalOffset { get; set; }
        public float Health { get; set; } = 100f;
        public bool IsDestroyed => Health <= 0f;
        public bool IsSmoking { get; set; } = false;

        public BossModularPart(Vector3 localOffset)
        {
            LocalOffset = localOffset;
        }

        public Vector3 ComputeWorldPosition(Vector3 bossPos, float bossAngleZ)
        {
            Matrix rot = Matrix.CreateRotationZ(bossAngleZ);
            return bossPos + Vector3.Transform(LocalOffset, rot);
        }

        public void TakeDamage(float damage)
        {
            Health = Math.Max(0f, Health - damage);
            if (IsDestroyed)
            {
                IsSmoking = true;
            }
        }
    }

    public class WorldBreakerBossModel
    {
        public Vector3 Position { get; set; }
        public float RotationZ { get; set; }
        public float CoreHealth { get; set; } = 1000f;
        public List<BossModularPart> Parts { get; } = new List<BossModularPart>();

        public WorldBreakerBossModel()
        {
            // 6 torretas
            Parts.Add(new BossModularPart(new Vector3(100f, 0f, 0f)));
            Parts.Add(new BossModularPart(new Vector3(-100f, 0f, 0f)));
            Parts.Add(new BossModularPart(new Vector3(0f, 80f, 0f)));
            Parts.Add(new BossModularPart(new BossModularPart(new Vector3(0f, -80f, 0f)).LocalOffset));
            Parts.Add(new BossModularPart(new Vector3(60f, 60f, 0f)));
            Parts.Add(new BossModularPart(new Vector3(-60f, -60f, 0f)));
        }
    }

    public class EclipseTitanBossModel
    {
        public float MaxHealth { get; set; } = 3000f;
        public float CurrentHealth { get; set; } = 3000f;
        public int Phase { get; private set; } = 1;
        public bool IsHealingCocoonActive { get; set; } = false;
        public bool IsVulnerablePostRitual { get; set; } = false;
        public int EscortCount { get; set; } = 4;

        public void UpdatePhase()
        {
            float ratio = CurrentHealth / MaxHealth;
            if (ratio > 0.66f)
            {
                Phase = 1;
            }
            else if (ratio > 0.33f)
            {
                Phase = 2;
            }
            else
            {
                Phase = 3;
                if (EscortCount > 0)
                {
                    IsHealingCocoonActive = true;
                }
            }
        }

        public void UpdateCocoonRegen(float dt)
        {
            if (Phase == 3 && IsHealingCocoonActive)
            {
                if (EscortCount > 0)
                {
                    // Cura 1% de HP por segundo
                    CurrentHealth = Math.Min(MaxHealth * 0.33f, CurrentHealth + (MaxHealth * 0.01f * dt));
                }
                else
                {
                    // Interrompe casulo ao zerar escoltas
                    IsHealingCocoonActive = false;
                    IsVulnerablePostRitual = true;
                }
            }
        }
    }

    public enum AsteroidTier
    {
        Huge,
        Large,
        Medium,
        Small
    }

    public class AsteroidModel
    {
        public AsteroidTier Tier { get; set; }
        public float Radius { get; set; }
        public float Mass { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }

        public static AsteroidModel CreateHuge(Vector2 pos, Vector2 vel)
        {
            return new AsteroidModel
            {
                Tier = AsteroidTier.Huge,
                Radius = 48f,
                Mass = 2500f,
                Position = pos,
                Velocity = vel
            };
        }

        public List<AsteroidModel> Fracture()
        {
            var fragments = new List<AsteroidModel>();
            switch (Tier)
            {
                case AsteroidTier.Huge:
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Large, Radius = 32f, Mass = 800f, Position = Position, Velocity = Velocity + new Vector2(30, 20) });
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Large, Radius = 32f, Mass = 800f, Position = Position, Velocity = Velocity + new Vector2(-30, -20) });
                    break;
                case AsteroidTier.Large:
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Medium, Radius = 20f, Mass = 250f, Position = Position });
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Medium, Radius = 20f, Mass = 250f, Position = Position });
                    break;
                case AsteroidTier.Medium:
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Small, Radius = 11f, Mass = 60f, Position = Position });
                    fragments.Add(new AsteroidModel { Tier = AsteroidTier.Small, Radius = 11f, Mass = 60f, Position = Position });
                    break;
            }
            return fragments;
        }

        public static void ResolveBilliardElasticImpact(AsteroidModel ast, EnemyModel enemy, out bool enemyCrushed)
        {
            // Choque elástico 2D com esmagamento cinético
            enemyCrushed = false;
            if (ast.Mass >= 2000f && ast.Velocity.Length() > 100f)
            {
                enemyCrushed = true;
                // Asteroide sofre desaceleração fracionária proporcional à massa
                ast.Velocity *= 0.85f;
            }
        }
    }
}
