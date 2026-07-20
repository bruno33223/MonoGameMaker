# `EntityManager` (Static Class)

The `EntityManager` class manages the life cycle of all active game entities. It handles spawning new entities, flagging entities for destruction, checking rectangle bounds collisions, executing behavior updates, updating sprite sheet animations, and propagating collision events.

---

## API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public static class EntityManager
    {
        public static List<GameEntity> Entities;
        public static ContentManager Content { get; set; }

        public static void Clear();
        public static GameEntity Spawn(string prefabName, Vector2 position);
        public static GameEntity CreateEntity();
        public static GameEntity RestoreEntity(Guid id);
        public static void Destroy(GameEntity entity);
        public static GameEntity GetFirstColliding(GameEntity caller, string targetTag);
        public static void Update(GameTime gameTime);
        public static void Draw(SpriteBatch spriteBatch, Texture2D defaultTexture);
        public static void DrawUI(SpriteBatch spriteBatch);
    }
}
```

---

## Life Cycle & Deferred Execution Model

To prevent `InvalidOperationException` (Collection Modified) errors, `EntityManager` manages entities using a deferred execution flow:

1.  **`Spawn(prefabName, position)`**: 
    Instantiates a `GameEntity` and its script, runs its `Awake` method, and adds it to a temporary deferred queue (`_entitiesToAdd`). It does NOT append directly to the active `Entities` list.
2.  **`CreateEntity()`**:
    Instantiates a new bare `GameEntity` with a freshly allocated `Guid` and adds it to the deferred queue (`_entitiesToAdd`), returning it.
3.  **`RestoreEntity(Guid id)`**:
    Instantiates a bare `GameEntity` by forcing the injection of the provided `Guid` (bypassing auto-generation) and adds it to the deferred queue (`_entitiesToAdd`), returning it. Primarily used for loading scenes and restoring state.
4.  **`Destroy(entity)`**: 
    Sets `entity.IsDestroyed = true`. The entity continues to exist for the duration of the current update frame.
3.  **`Update(gameTime)`**:
    - At the start of the tick, it moves all deferred spawned entities from the queue into the active `Entities` list.
    - It updates all active, non-destroyed entity script logic loops and advances active animations (`UpdateAnimation`).
    - **Pairwise Collision Check**: Executes automatic boundary collision triggers.
    - At the end of the tick, it removes all entities where `IsDestroyed == true` using `Entities.RemoveAll(e => e.IsDestroyed)`.

This guarantees loop safety, allowing any script to spawn or destroy entities safely during their `Update` or `OnCollision` calls.

---

## Pairwise Collision System (Events)

In addition to manual `GetFirstColliding` queries, the `EntityManager` runs an automatic O(N^2) pairwise AABB intersection scan at the end of every update tick:

```csharp
for (int i = 0; i < Entities.Count; i++)
{
    var entA = Entities[i];
    if (entA.IsDestroyed) continue;
    for (int j = i + 1; j < Entities.Count; j++)
    {
        var entB = Entities[j];
        if (entB.IsDestroyed) continue;
        if (entA.Bounds.Intersects(entB.Bounds))
        {
            entA.Script?.OnCollision(entB);
            entB.Script?.OnCollision(entA);
        }
    }
}
```

If any two non-destroyed entities overlap, the manager calls `OnCollision(other)` on both script instances, allowing scripts to react to collisions without manual polling.

---

## Practical Example: Weapon Spawn & Collision Event Loop

### 1. Firing Projectiles (Player Script)
```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class PlayerShip : EntityBehavior
    {
        private float _fireCooldown = 0f;

        public override void Update(GameTime gameTime)
        {
            _fireCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Space) && _fireCooldown <= 0f)
            {
                // Spawn projectile entity slightly in front of player
                Vector2 spawnPos = new Vector2(Entity.Position.X + 32, Entity.Position.Y + 16);
                EntityManager.Spawn("laser_bullet", spawnPos);

                _fireCooldown = 0.25f; // Cooldown of 250ms
            }
        }
    }
}
```

### 2. Handling Collisions (Bullet Script)
```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class LaserBullet : EntityBehavior
    {
        private float _speed = 500f;
        private float _lifeTime = 2f;

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Move right
            Entity.Position = new Vector2(Entity.Position.X + (_speed * dt), Entity.Position.Y);

            // Destroy bullet after lifetime expires
            _lifeTime -= dt;
            if (_lifeTime <= 0f)
            {
                EntityManager.Destroy(Entity);
            }
        }

        // Triggered automatically by the EntityManager
        public override void OnCollision(GameEntity other)
        {
            if (other.Tag == "Enemy")
            {
                // Destroy enemy and bullet
                EntityManager.Destroy(other);
                EntityManager.Destroy(Entity);
                
                // Add points
                int currentPoints = GameState.Get<int>("Score", 0);
                GameState.Set("Score", currentPoints + 100);
            }
        }
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to handle entity updates, collision reactions, or spawning:

*   **Prompt Scenario**: *"Write an EntityBehavior script for an enemy. Override OnCollision to destroy the player when hit."*
*   **Prompt Scenario**: *"Implement a trigger script. When a player entity collides with it, load level_2."*
*   **Key Guardrail**: Avoid writing custom collision detection loops inside `Update()`. Instead, set the entity bounds and tags in the editor and react to collisions by overriding `OnCollision(other)`.
