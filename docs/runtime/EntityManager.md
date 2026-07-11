# `EntityManager` (Static Class)

The `EntityManager` class manages the life cycle of all active game entities. It handles spawning new entities, flagging entities for destruction, checking rectangle bounds collisions, and executing behavior updates.

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
    Instantiates a `GameEntity` and its script, runs its `Initialize` method, and adds it to a temporary deferred queue (`_entitiesToAdd`). It does NOT append directly to the active `Entities` list.
2.  **`Destroy(entity)`**: 
    Sets `entity.IsDestroyed = true`. The entity continues to exist for the duration of the current update frame.
3.  **`Update(gameTime)`**:
    - At the start of the tick, it moves all deferred spawned entities from the queue into the active `Entities` list.
    - It updates all active, non-destroyed entity script logic loops.
    - At the end of the tick, it removes all entities where `IsDestroyed == true` using `Entities.RemoveAll(e => e.IsDestroyed)`.

This guarantees loop safety, allowing any script to spawn or destroy entities safely during their `Update` calls.

---

## Collision Engine (AABB)

The `GetFirstColliding` method uses Axis-Aligned Bounding Box (AABB) intersection check. It checks the calculated `Bounds` rectangle of the caller against every active, non-destroyed entity matching the specified tag:

```csharp
public static GameEntity GetFirstColliding(GameEntity caller, string targetTag)
{
    foreach (var entity in Entities)
    {
        if (entity != caller && !entity.IsDestroyed && entity.Tag == targetTag)
        {
            if (caller.Bounds.Intersects(entity.Bounds))
            {
                return entity;
            }
        }
    }
    return null;
}
```

---

## Practical Example: Weapon Spawn & Collide Loop

Below is a complete implementation of a player projectile script spawning bullets, and the bullet script detecting and destroying enemy objects:

### 1. Firing Projectiles (Player Script)
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class PlayerShip : IEntityScript
    {
        private GameEntity _entity;
        private float _fireCooldown = 0f;

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
        }

        public void Update(GameTime gameTime)
        {
            _fireCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Space) && _fireCooldown <= 0f)
            {
                // Spawn projectile entity slightly in front of player
                Vector2 spawnPos = new Vector2(_entity.Position.X + 32, _entity.Position.Y + 16);
                EntityManager.Spawn("laser_bullet", spawnPos);

                _fireCooldown = 0.25f; // Cooldown of 250ms
            }
        }

        public void Draw(SpriteBatch spriteBatch) {}
        public void DrawUI(SpriteBatch spriteBatch) {}
    }
}
```

### 2. Colliding & Destroying (Bullet Script)
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class LaserBullet : IEntityScript
    {
        private GameEntity _entity;
        private float _speed = 500f;
        private float _lifeTime = 2f;

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Move right
            _entity.Position = new Vector2(_entity.Position.X + (_speed * dt), _entity.Position.Y);

            // Destroy bullet after lifetime expires
            _lifeTime -= dt;
            if (_lifeTime <= 0f)
            {
                EntityManager.Destroy(_entity);
                return;
            }

            // Check AABB collision against enemies
            GameEntity collidedEnemy = EntityManager.GetFirstColliding(_entity, "Enemy");
            if (collidedEnemy != null)
            {
                // Destroy enemy and bullet
                EntityManager.Destroy(collidedEnemy);
                EntityManager.Destroy(_entity);
                
                // Add points
                int currentPoints = GameState.Get<int>("Score", 0);
                GameState.Set("Score", currentPoints + 100);
            }
        }

        public void Draw(SpriteBatch spriteBatch) {}
        public void DrawUI(SpriteBatch spriteBatch) {}
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to handle entity spawning, destruction, or collision checks:

*   **Prompt Scenario**: *"Write a script for an enemy hazard that detects collisions with the player. If they collide, destroy the player entity and restart the level."*
*   **Prompt Scenario**: *"Create a script that spawns gold coin prefabs at random coordinates every 3 seconds, keeping track of spawn limits."*
*   **Key Guardrail**: Never manually add or remove objects from the `EntityManager.Entities` list directly inside update loops. Always use `EntityManager.Spawn` and `EntityManager.Destroy` to queue modification tasks safely.
