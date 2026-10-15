# `ObjectPool<T>` Manual da API

O `ObjectPool<T>` é o gerenciador genérico, thread-safe e de zero alocações de memória heap projetado para reciclagem contínua de objetos que implementam o contrato `IPoolable`. Operando sob uma pilha interna LIFO (*Last-In, First-Out*), ele maximiza o aproveitamento do cache L1/L2 do processador e elimina totalmente chamadas a `new` durante o combate ativo.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pooling
{
    /// <summary>
    /// Gerenciador de Object Pooling genérico, thread-safe e de zero alocações de GC para objetos IPoolable.
    /// Utiliza uma coleção interna LIFO (Stack) para maximizar o reuso de memória e manter instâncias quentes no cache de CPU.
    /// </summary>
    /// <typeparam name="T">Tipo de referência que implementa o contrato IPoolable.</typeparam>
    public class ObjectPool<T> where T : class, IPoolable
    {
        public int Count { get; }

        public ObjectPool(Func<T>? factory = null, int initialCapacity = 64);

        /// <summary>
        /// Pré-aquece o pool instanciando em lote a quantidade solicitada de objetos.
        /// </summary>
        public void PreWarm(int count);

        /// <summary>
        /// Obtém uma instância pronta para uso do pool executando OnSpawn().
        /// </summary>
        public T Get();

        /// <summary>
        /// Alias idiomático de alta performance para Get().
        /// </summary>
        public T Rent();

        /// <summary>
        /// Devolve uma instância utilizada ao pool executando OnDespawn().
        /// </summary>
        public void Release(T obj);

        /// <summary>
        /// Alias idiomático de alta performance para Release().
        /// </summary>
        public void Return(T obj);

        /// <summary>
        /// Limpa todas as instâncias em repouso no pool.
        /// </summary>
        public void Clear();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Pré-aquecimento Balístico (`PreWarm`):** No carregamento de cada setor de combate, inicialize 512 projéteis de laser e 128 fragmentos de asteroides. Durante o combate, nenhum ciclo de garbage collection é disparado.
2. **Reuso de Instâncias $O(1)$:** `Get()` e `Release()` operam em tempo constante sobre a coleção interna protegida por `lock`, garantindo segurança concorrente caso sistemas assíncronos ou threads de partículas acessem o pool.
3. **Padrão Oficial de Substituição de `new`:** IAs e desenvolvedores nunca devem instanciar tiros ou partículas soltas no método `Update()`; devem sempre resgatar do pool via `Rent()` / `Get()` e devolver via `Return()` / `Release()`.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra o ciclo completo de pré-aquecimento, disparo e reciclagem de projéteis em um sistema de combate do MonoGame:

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Pooling;

namespace MyGame.Combat
{
    public class WeaponSystem
    {
        private readonly ObjectPool<PlasmaProjectile> _bulletPool;
        private readonly List<PlasmaProjectile> _activeBullets;

        public WeaponSystem()
        {
            // Instancia o pool e pré-aquece 256 instâncias no heap
            _bulletPool = new ObjectPool<PlasmaProjectile>(() => new PlasmaProjectile(), initialCapacity: 256);
            _bulletPool.PreWarm(256);
            _activeBullets = new List<PlasmaProjectile>(256);
        }

        public void Fire(Vector2 spawnPosition, Vector2 direction, float speed)
        {
            // Zero-GC: Retira do pool já higienizado via OnSpawn()
            PlasmaProjectile bullet = _bulletPool.Get();
            bullet.Position = spawnPosition;
            bullet.Velocity = direction * speed;
            _activeBullets.Add(bullet);
        }

        public void Update(float dt)
        {
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                var bullet = _activeBullets[i];
                bullet.Update(dt, _bulletPool);

                // Se o projétil expirou e se desativou, remove da lista de ativos
                if (!bullet.IsActive)
                {
                    _activeBullets.RemoveAt(i);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            for (int i = 0; i < _activeBullets.Count; i++)
            {
                _activeBullets[i].Draw(spriteBatch, texture);
            }
        }
    }
}
```
