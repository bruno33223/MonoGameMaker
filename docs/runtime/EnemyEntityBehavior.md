# `EnemyEntityBehavior` Manual da API

O `EnemyEntityBehavior` é a classe base abstrata para todos os 43 arquétipos de naves hostis e ameaças do ecossistema de combate do MonoGameMaker. Herdando de `EntityBehavior`, ele se integra diretamente com o `EntityManager` e o Inspector com Reflection do editor, provendo propriedades padronizadas de combate (vida, dano, raio de colisão e valor de pontuação), processamento de dano mitigado e despacho limpo do ciclo de morte.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Combat
{
    public abstract class EnemyEntityBehavior : EntityBehavior
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Radius { get; set; }
        public float Damage { get; set; }
        public int ScoreValue { get; set; }
        public bool IsDead { get; }

        public virtual void TakeDamage(float amount, CombatContext ctx);
        public abstract void UpdateCombat(CombatContext ctx);
        public abstract void Die(CombatContext ctx);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Substituição de Liskov em Todos os Inimigos:** Quer seja um drone kamikaze rápido (`ScarabKamikaze`), um atirador de ácido biológico (`ToxisBioSpit`) ou um cruzador couraçado, todas as unidades compartilham o mesmo contrato de dano e atualização de combate.
2. **Integração com o Inspector do Editor:** Campos públicos como `Health`, `Damage` e `Radius` são automaticamente inspecionáveis e editáveis em tempo real via Reflection no painel do editor do MonoGameMaker.
3. **Consumo Coeso do Contexto:** A IA da nave hostil recebe `CombatContext`, tendo acesso direto às coordenadas da nave do jogador (`ctx.PlayerPosition`) para cálculo de perseguição e mira preditiva sem acoplamento a objetos globais.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a implementação de um drone caçador hostil herdando de `EnemyEntityBehavior`:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;
using MonoGameMaker.Runtime.Combat;

namespace MyGame.Enemies
{
    public class ChaserDroneBehavior : EnemyEntityBehavior
    {
        public float MoveSpeed { get; set; } = 280f;

        public override void Awake()
        {
            MaxHealth = 80f;
            Health = 80f;
            Radius = 20f;
            Damage = 25f;
            ScoreValue = 250;
        }

        public override void UpdateCombat(CombatContext ctx)
        {
            if (IsDead || !ctx.IsPlayerAlive || Entity == null) return;

            // Persegue a posição do jogador fornecida pelo CombatContext
            Vector2 currentPos = Entity.Position;
            Vector2 targetPos = new Vector2(ctx.PlayerPosition.X, ctx.PlayerPosition.Y);
            Vector2 toPlayer = targetPos - currentPos;

            if (toPlayer.LengthSquared() > 1f)
            {
                Vector2 dir = Vector2.Normalize(toPlayer);
                Entity.Position += dir * MoveSpeed * ctx.DeltaTime;
                Entity.Rotation = MathF.Atan2(dir.Y, dir.X);
            }
        }

        public override void Die(CombatContext ctx)
        {
            // Emite efeitos de morte através dos serviços do contexto
            ctx.Audio?.Play(SoundType.ExplosionSmall, volume: 0.8f);
            ctx.Camera?.AddTrauma(0.15f);

            Vector3 worldPos = new Vector3(Entity?.Position.X ?? 0f, Entity?.Position.Y ?? 0f, 0f);
            ctx.Particles?.EmitExplosion(worldPos, count: 25, startColor: Color.Orange, speed: 150f);

            // Incrementa score da run
            ctx.OnScoreAdded?.Invoke(ScoreValue);

            // Marca a entidade para descarte seguro no sweep do EntityManager
            Entity?.Destroy();
        }
    }
}
```
