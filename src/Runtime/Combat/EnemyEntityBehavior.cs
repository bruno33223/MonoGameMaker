using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Core;

namespace MonoGameMaker.Runtime.Combat
{
    /// <summary>
    /// Comportamento base para todas as naves e entidades hostis da simulação de combate.
    /// Provê encapsulamento para atributos vitais (Health, MaxHealth, Radius, Damage, ScoreValue),
    /// processamento de dano mitigado e despacho limpo do ciclo de morte.
    /// </summary>
    public abstract class EnemyEntityBehavior : EntityBehavior
    {
        public float Health { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public float Radius { get; set; } = 24f;
        public float Damage { get; set; } = 20f;
        public int ScoreValue { get; set; } = 100;
        public bool IsDead => Health <= 0;

        /// <summary>
        /// Aplica dano à entidade e engatilha a rotina de morte se os pontos de vida zerarem.
        /// </summary>
        public virtual void TakeDamage(float amount, CombatContext ctx)
        {
            if (IsDead) return;

            Health -= amount;
            if (Health <= 0)
            {
                Health = 0;
                Die(ctx);
            }
        }

        /// <summary>
        /// Atualiza a lógica de combate ativa (movimentação, mira, recuo e disparos) consumindo o CombatContext sem parameter tunneling.
        /// </summary>
        public abstract void UpdateCombat(CombatContext ctx);

        /// <summary>
        /// Executa a rotina de morte da entidade (emissão de partículas, som de explosão e pontuação).
        /// </summary>
        public abstract void Die(CombatContext ctx);
    }
}
