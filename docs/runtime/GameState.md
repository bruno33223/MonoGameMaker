# `GameState` (Static Class)

The `GameState` static class provides a persistent, global key-value data store that survives scene changes. It is used to share data (like health, score, or inventory items) between entities or keep stats when transitioning levels.

---

## API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public static class GameState
    {
        public static Dictionary<string, object> Data;

        public static void Set<T>(string key, T value);
        public static T Get<T>(string key, T defaultValue = default);
    }
}
```

---

## Generic Data Storage & Type Conversion

The `GameState` dictionary stores value types as raw `object` instances. When retrieving variables, it runs a fail-safe conversion process:

1.  **`Set<T>(key, value)`**:
    - If `value` is `null`, the key is removed from the storage dictionary.
    - Otherwise, the value is written to `Data[key]`.
2.  **`Get<T>(key, defaultValue)`**:
    - If the key does not exist, returns `defaultValue`.
    - Attempts to run `Convert.ChangeType(val, typeof(T))` to safely convert numerical types (e.g. from `double` to `int`).
    - If explicit conversion fails, attempts to cast directly `(T)val`.
    - If both attempts throw exceptions, it silently catches the failure and returns the specified `defaultValue`.

This ensures type-safe retrieval even when variables are written as different numerical configurations (e.g., loaded from save states).

---

## Practical Example: Persisting Health & Inventory Across Levels

Below is a complete script setup demonstrating how a player script updates health and inventory items, and preserves them during a level transition:

### 1. Saving Player Status
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class PlayerStatus : IEntityScript
    {
        private GameEntity _entity;
        
        // Runtime stats
        private int _currentHealth;
        private List<string> _inventory = new List<string>();

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;

            // Load existing stats from GameState, or use defaults
            _currentHealth = GameState.Get<int>("Health", 100);
            _inventory = GameState.Get<List<string>>("InventoryItems", new List<string>());
        }

        public void Update(GameTime gameTime)
        {
            // Simulate picking up items
            var collidedItem = EntityManager.GetFirstColliding(_entity, "PickupItem");
            if (collidedItem != null)
            {
                string itemName = collidedItem.PrefabName;
                _inventory.Add(itemName);
                
                // Update persistent GameState
                GameState.Set("InventoryItems", _inventory);

                // Destroy item from scene
                EntityManager.Destroy(collidedItem);
            }

            // Simulate taking damage
            var hazard = EntityManager.GetFirstColliding(_entity, "Hazard");
            if (hazard != null)
            {
                _currentHealth -= 10;
                
                // Update persistent GameState
                GameState.Set("Health", _currentHealth);

                if (_currentHealth <= 0)
                {
                    // Game Over logic
                    SceneManager.LoadScene("game_over");
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch) {}
        public void DrawUI(SpriteBatch spriteBatch) {}
    }
}
```

### 2. Rendering Health Overlay (UI Script)
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class HealthBarHud : IEntityScript
    {
        private GameEntity _entity;
        private SpriteFont _font;

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
            if (EntityManager.Content != null)
            {
                _font = EntityManager.Content.Load<SpriteFont>("Fonts/HudFont");
            }
        }

        public void Update(GameTime gameTime) {}
        public void Draw(SpriteBatch spriteBatch) {}

        public void DrawUI(SpriteBatch spriteBatch)
        {
            // Query current health from GameState
            int health = GameState.Get<int>("Health", 100);
            
            // Draw simple health text on screen
            if (_font != null)
            {
                spriteBatch.DrawString(_font, $"HP: {health}/100", new Vector2(20, 50), Color.Red);
            }
        }
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to handle game states:

*   **Prompt Scenario**: *"Write a coin pickup script that increments a Score integer in GameState by 50 when collected by the player."*
*   **Prompt Scenario**: *"Implement a shop system script that checks if the PlayerGold integer inside GameState is greater than 100 before deducting gold and adding an item to the Inventory list."*
*   **Key Guardrail**: Always provide a sensible fallback default value inside `GameState.Get<T>()` to prevent null dereferences or type mismatch errors if the variable has not been initialized.
