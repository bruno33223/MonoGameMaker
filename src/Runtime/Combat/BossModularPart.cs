using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;
using MonoGameMaker.Runtime.Core;

namespace MonoGameMaker.Runtime.Combat
{
    /// <summary>
    /// Comportamento modular para sub-partes anatômicas de chefes épicos (torretas, asas, casulos de regeneração, canhões).
    /// Mantém pontos de vida próprios, hitbox independente e cálculo de posição relativa à carcaça pai do chefe:
    /// P_world = P_boss + Rotate(LocalOffset, BossRotation).
    /// </summary>
    public class BossModularPart : EntityBehavior
    {
        public string PartId { get; set; } = "Turret";
        public Vector2 LocalOffset { get; set; } = Vector2.Zero;
        public float Health { get; set; } = 5000f;
        public float MaxHealth { get; set; } = 5000f;
        public float Radius { get; set; } = 32f;
        public bool IsDestroyed => Health <= 0;
        public float DestructionSmokeTimer { get; set; } = 0f;

        /// <summary>
        /// Aplica dano localizado à parte modular. Ao atingir 0 HP, dispara o efeito de quebra de componente.
        /// </summary>
        public virtual void TakeDamage(float amount, CombatContext ctx)
        {
            if (IsDestroyed) return;

            Health -= amount;
            if (Health <= 0)
            {
                Health = 0;
                OnPartDestroyed(ctx);
            }
        }

        /// <summary>
        /// Atualiza as coordenadas globais da parte modular com base na posição e ângulo da nave chefe.
        /// </summary>
        public virtual void UpdatePart(CombatContext ctx, Vector2 bossPosition, float bossRotation)
        {
            float cos = MathF.Cos(bossRotation);
            float sin = MathF.Sin(bossRotation);

            Vector2 rotatedOffset = new Vector2(
                LocalOffset.X * cos - LocalOffset.Y * sin,
                LocalOffset.X * sin + LocalOffset.Y * cos
            );

            if (Entity != null)
            {
                Entity.Position = bossPosition + rotatedOffset;
                Entity.Rotation = bossRotation;
            }

            if (IsDestroyed)
            {
                // Emite fumaça contínua
                DestructionSmokeTimer += ctx.DeltaTime;
                if (DestructionSmokeTimer >= 0.08f)
                {
                    DestructionSmokeTimer = 0f;
                    Vector3 worldPos3D = new Vector3(Entity?.Position.X ?? bossPosition.X, Entity?.Position.Y ?? bossPosition.Y, 0f);
                    ctx.Particles?.EmitSparks(worldPos3D, Vector3.UnitY, 45f, 3, Color.DimGray);
                }
            }
        }

        /// <summary>
        /// Hook invocado quando a parte modular tem sua vida zerada.
        /// </summary>
        protected virtual void OnPartDestroyed(CombatContext ctx)
        {
            ctx.Audio?.Play(SoundType.TitanDestructionSub, 0.90f);
            ctx.Camera?.AddTrauma(0.35f);

            Vector3 worldPos3D = new Vector3(Entity?.Position.X ?? 0f, Entity?.Position.Y ?? 0f, 0f);
            ctx.Particles?.EmitExplosion(worldPos3D, count: 40, startColor: Color.OrangeRed, endColor: Color.DarkSlateGray, speed: 180f);
        }
    }
}
