# `EntityBehavior` (Abstract Class)

The `EntityBehavior` abstract class replaces the old `IEntityScript` interface as the foundation for custom entity logic in Mono GameMaker. It provides default virtual method implementations, allowing scripts to implement only the methods they need, reducing boilerplate code.

---

## API Definition

```csharp
namespace MonoGameMaker.Runtime
{
    public abstract class EntityBehavior
    {
        public GameEntity Entity { get; internal set; }
        public Dictionary<string, string> Properties { get; internal set; }

        public virtual void Awake() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnCollision(GameEntity other) { }
    }
}
```

### Properties
- **`Entity`** (`GameEntity`): The parent entity instance this script is attached to.
- **`Properties`** (`Dictionary<string, string>`): The custom parameters passed from the scene layout instance or prefab configuration.

---

## Lifecycle Events

1.  **`Awake()`**
    *   **Triggered**: Immediately after the entity is instantiated and properties are assigned.
    *   **Purpose**: Used to initialize variables, read configuration properties, cache references to other objects, or trigger animations.
2.  **`Update(GameTime gameTime)`**
    *   **Triggered**: Every frame cycle inside the game logic loop.
    *   **Purpose**: Handles input reading, movement velocity adjustments, and cooldown timers.
3.  **`Draw(SpriteBatch spriteBatch)`**
    *   **Triggered**: During the World Space rendering pass.
    *   **Purpose**: Optional custom world rendering. If left un-overridden, the engine automatically draws the entity's texture (or current animation frame clip) at its position.
4.  **`DrawUI(SpriteBatch spriteBatch)`**
    *   **Triggered**: During the Screen Space UI rendering pass.
    *   **Purpose**: Renders heads-up displays, score text, or fixed interface overlays.
5.  **`OnCollision(GameEntity other)`**
    *   **Triggered**: Automatically whenever the AABB bounds of this entity intersect with the bounds of another entity.
    *   **Purpose**: Handles collision reactions (e.g. taking damage, picking up items, applying physics recoil) without requiring manual coordinates parsing in the `Update` loop.

---

## Practical Example: Zero-Boilerplate Collision Reaction

Below is a complete script demonstrating how clean behavior files become. A script can override only `OnCollision` and ignore `Update` and `Draw` entirely:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class MineHazard : EntityBehavior
    {
        public override void OnCollision(GameEntity other)
        {
            // If we collide with a Player, destroy the mine
            if (other.Tag == "Player")
            {
                Console.WriteLine("Boom! Mine detonated.");
                EntityManager.Destroy(Entity);
            }
        }
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to create script behaviors using `EntityBehavior`:

*   **Prompt Scenario**: *"Create an EntityBehavior script named PickupCoin that inherits from EntityBehavior. Override only OnCollision to increment global coins in GameState and destroy the coin entity when touched by the player."*
*   **Prompt Scenario**: *"Write an EntityBehavior that updates coordinate positions on Update and does not declare Awake or Draw."*
*   **Key Guardrail**: Do not declare custom constructors. Initialization code should always be written inside the overridden `Awake()` method.
