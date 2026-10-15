# `IPoolable` Manual da API

O contrato `IPoolable` é a interface mandatória para todas as entidades, projéteis, efeitos visuais e partículas recicláveis no runtime do MonoGameMaker. Ele governa o ciclo de vida de ativação e hibernação de objetos em pools de memória pré-aquecidos, viabilizando a política inegociável de **Zero-GC** no loop de combate.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pooling
{
    /// <summary>
    /// Contrato estrito para objetos recicláveis em ObjectPools de alta performance com Zero-GC.
    /// Define os hooks de ciclo de vida para inicialização e higienização de estado.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Indica se a instância está atualmente ativa na simulação do jogo.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Chamado imediatamente quando o objeto é retirado do pool para reuso.
        /// Deve reabilitar visibilidade, física e resetar flags essenciais.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Chamado imediatamente quando o objeto é devolvido ao pool.
        /// Deve desabilitar visibilidade, física e limpar referências pendentes.
        /// </summary>
        void OnDespawn();

        /// <summary>
        /// Reseta o estado interno do objeto para os valores de repouso.
        /// </summary>
        void Reset() => OnDespawn();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Eliminação de Garbage Collection em Tempo de Execução:** Em combates espaciais intensos (60 FPS com centenas de disparos de laser, estilhaços e drops de almas simultâneos), alocar instâncias via `new` causa pausas de coleta de lixo (GC spikes). Implementando `IPoolable`, a memória de cada projétil é pré-alocada no boot e reutilizada indefinidamente.
2. **Higienização Determinística de Estado:** Ao ser reciclada, uma entidade não pode reter velocidade residual, vida anterior ou referências antigas de alvos. O método `OnDespawn()` assegura o isolamento de estado antes do retorno à pilha.
3. **Padrão Homogêneo para Sistemas e Spawners:** Qualquer gerenciador balístico ou emissor de detritos interage genericamente com `IPoolable`, desconhecendo detalhes concretos do prefab ou malha subjacente.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra a implementação de um projétil de plasma reutilizável gerenciado por um pool de memória:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Pooling;

namespace MyGame.Combat
{
    public class PlasmaProjectile : IPoolable
    {
        public bool IsActive { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Lifetime { get; private set; }
        public float MaxLifetime { get; set; } = 2.5f;
        public float Damage { get; set; } = 45f;

        public void OnSpawn()
        {
            IsActive = true;
            Lifetime = 0f;
        }

        public void OnDespawn()
        {
            IsActive = false;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Lifetime = 0f;
        }

        public void Update(float dt, ObjectPool<PlasmaProjectile> pool)
        {
            if (!IsActive) return;

            Position += Velocity * dt;
            Lifetime += dt;

            // Ao expirar ou ultrapassar a tela, devolve ao pool sem gerar GC
            if (Lifetime >= MaxLifetime)
            {
                pool.Release(this);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (!IsActive) return;
            spriteBatch.Draw(texture, Position, Color.Cyan);
        }
    }
}
```
