# `BossModularPart` Manual da API

O `BossModularPart` é o componente modular de entidade projetado para gerenciar as sub-partes anatômicas dos 12 chefes titânicos do MonoGameMaker (como torretas de disparo independente, asas de blindagem destacável e núcleos de sustentação). Operando como uma entidade filha no `EntityManager`, ele calcula sua posição relativa no espaço a partir da matriz de rotação da nave-mãe, mantém hitbox e vida individuais, e ao ser destruído emite efeitos de quebra e fumaça sem destruir o chefe pai.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Combat
{
    public class BossModularPart : EntityBehavior
    {
        public string PartId { get; set; }
        public Vector2 LocalOffset { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Radius { get; set; }
        public bool IsDestroyed { get; }
        public float DestructionSmokeTimer { get; set; }

        public virtual void TakeDamage(float amount, CombatContext ctx);
        public virtual void UpdatePart(CombatContext ctx, Vector2 bossPosition, float bossRotation);
        protected virtual void OnPartDestroyed(CombatContext ctx);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Chefes com Múltiplos Pontos de Colisão e Destruição:** Em chefes massivos (ex: *WorldBreaker Titan* com 180px de largura), uma única hitbox esférica é inadequada. Sub-partes permitem destruir a torreta de plasma esquerda antes de avançar para o reator central.
2. **Cálculo Angular de Offset Local:**
   $$\vec{P}_{\text{world}} = \vec{P}_{\text{boss}} + \begin{pmatrix} \cos\theta & -\sin\theta \\ \sin\theta & \cos\theta \end{pmatrix} \vec{Offset}_{\text{local}}$$
   O método `UpdatePart` aplica a rotação automaticamente a cada quadro.
3. **Persistência Visual de Ruína:** Ao atingir 0 HP, a sub-parte não desaparece; ela emite som de colapso metálico pesado (`TitanDestructionSub`), adiciona trauma à câmera e passa a emitir partículas volumétricas de fumaça preta contínua.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a montagem de uma torreta modular atrelada a uma carcaça de chefe:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Combat;

namespace MyGame.Bosses
{
    public class HeavyTurretPart : BossModularPart
    {
        private float _fireCooldown = 0f;

        public override void Awake()
        {
            PartId = "LeftPlasmaTurret";
            LocalOffset = new Vector2(-60f, -30f);
            MaxHealth = 3500f;
            Health = 3500f;
            Radius = 28f;
        }

        public override void UpdatePart(CombatContext ctx, Vector2 bossPosition, float bossRotation)
        {
            base.UpdatePart(ctx, bossPosition, bossRotation);

            // Se ainda não estiver destruída, executa ciclo de disparo autônomo
            if (!IsDestroyed && ctx.IsPlayerAlive)
            {
                _fireCooldown -= ctx.DeltaTime;
                if (_fireCooldown <= 0f)
                {
                    _fireCooldown = 1.2f;
                    // Dispara projétil na direção do jogador a partir de Entity.Position
                }
            }
        }
    }
}
```
