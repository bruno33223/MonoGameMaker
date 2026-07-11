# Save/Load Persistence System Manual

This manual details the JSON-based serialization and deserialization APIs used to persist game progression inside `GameState`.

---

## 1. High-Level Description
The save system resolves the problem of saving and loading player progress (e.g. current level, score, inventory, unlocked traits) to disk. The `GameState` data is serialized as a JSON document to a file named `save_state.json` (or a custom file name) located in the game's executable path.
Upon loading, the JSON is read, parsed, and converted back to a key-value dictionary. Values are automatically mapped back to primitive types (`int`, `double`, `string`, `bool`), allowing type-safe queries using `GameState.Get<T>()`.

---

## 2. API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public static class GameState
    {
        /// <summary>
        /// Serializes and writes current state variables to a JSON file.
        /// </summary>
        /// <param name="filename">The relative or absolute file path. Defaults to "save_state.json".</param>
        public static void SaveToFile(string filename = "save_state.json");

        /// <summary>
        /// Reads, deserializes, and loads state variables from a JSON file.
        /// </summary>
        /// <param name="filename">The relative or absolute file path. Defaults to "save_state.json".</param>
        public static void LoadFromFile(string filename = "save_state.json");
    }
}
```

---

## 3. Use Cases (Context for AI Assistants)
- **Checkpoints / Level Transition**: Trigger `GameState.SaveToFile()` whenever a player reaches a checkpoint or completes a level.
- **Boot Loading**: Call `GameState.LoadFromFile()` in the boot level or first script `Awake()` method to restore health, stats, and coordinates before spawning objects.
- **Multiple Save Slots**: Pass distinct slot names (e.g. `"save_slot_1.json"`, `"save_slot_2.json"`) to support multiple save profiles.

---

## 4. Practical C# Compileable Example

The following script illustrates how to construct a complete Save/Load controller behavior that registers input keys (`F5` for Quick Save, `F9` for Quick Load):

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class SaveGameManager : EntityBehavior
    {
        private bool _f5PressedLastFrame = false;
        private bool _f9PressedLastFrame = false;

        public override void Awake()
        {
            // Load progress at start
            GameState.LoadFromFile("save_state.json");
            int score = GameState.Get<int>("Score", 0);
            int coins = GameState.Get<int>("Coins", 0);
            GlobalState.Log($"[SaveGameManager] Loaded game. Score: {score}, Coins: {coins}");
        }

        public override void Update(GameTime gameTime)
        {
            var keyState = Keyboard.GetState();

            // Quick Save Trigger (F5)
            if (keyState.IsKeyDown(Keys.F5))
            {
                if (!_f5PressedLastFrame)
                {
                    // Update state values before saving
                    int currentScore = GameState.Get<int>("Score", 0) + 10; // increment score for demonstration
                    GameState.Set("Score", currentScore);
                    GameState.Set("Coins", 25);
                    
                    GameState.SaveToFile("save_state.json");
                    GlobalState.Log("[SaveGameManager] Quick Saved to save_state.json!");
                }
                _f5PressedLastFrame = true;
            }
            else
            {
                _f5PressedLastFrame = false;
            }

            // Quick Load Trigger (F9)
            if (keyState.IsKeyDown(Keys.F9))
            {
                if (!_f9PressedLastFrame)
                {
                    GameState.LoadFromFile("save_state.json");
                    int score = GameState.Get<int>("Score", 0);
                    int coins = GameState.Get<int>("Coins", 0);
                    GlobalState.Log($"[SaveGameManager] Quick Loaded! Score: {score}, Coins: {coins}");
                }
                _f9PressedLastFrame = true;
            }
            else
            {
                _f9PressedLastFrame = false;
            }
        }
    }
}
```
