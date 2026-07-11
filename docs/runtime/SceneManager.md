# `SceneManager` (Static Class)

The `SceneManager` class manages scene loading, resource scoping, and transitions. It coordinates clearing entity cache matrices, loading new layout maps, and configuring active game boundaries.

---

## API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public static class SceneManager
    {
        public static string CurrentSceneName { get; }
        public static RuntimeScene CurrentScene { get; }

        public static void Initialize(ContentManager content);
        public static void LoadScene(string sceneName);
    }
}
```

---

## Level Transitions & Bootstrap Cycle

1.  **`Initialize(content)`**:
    Injects the active `ContentManager` reference into the Scene Manager. It configures `EntityManager.Content` so entity scripts can load textures, audio clips, and fonts during runtime initialization.
2.  **`LoadScene(sceneName)`**:
    - Purges the `EntityManager` list via `EntityManager.Clear()`.
    - Sanitizes the input string name, appending `"Scenes/"` path prefix and `".json"` file extension if omitted.
    - Resolves the absolute path and reads the JSON file layout using `SceneLoader.LoadScene`.
    - Configures active room dimensions (`CurrentScene.Width`, `CurrentScene.Height`).
    - Configures clear color values and background texture overlays.
    - Adds the deserialized entities list directly to `EntityManager.Entities`.
    - Resets `Camera2D.Position` to `Vector2.Zero`.

---

## Practical Example: Goal Level Transition Portal Script

Below is a complete implementation of a level portal script. When the player entity intersects the portal bounds, it triggers a transition to the next phase:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class GoalPortal : IEntityScript
    {
        private GameEntity _entity;
        private string _targetScene = "level_2";

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
            if (properties.TryGetValue("TargetScene", out var target))
            {
                _targetScene = target;
            }
        }

        public void Update(GameTime gameTime)
        {
            // Scan for player collisions
            GameEntity player = EntityManager.GetFirstColliding(_entity, "Player");
            if (player != null)
            {
                // Trigger scene load transition
                Console.WriteLine($"Player entered portal. Transitioning to {_targetScene}...");
                SceneManager.LoadScene(_targetScene);
            }
        }

        public void Draw(SpriteBatch spriteBatch) {}
        public void DrawUI(SpriteBatch spriteBatch) {}
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to manage level assets or handle transitions:

*   **Prompt Scenario**: *"Write a trigger script that loads the 'game_over' scene if the player falls off the bottom edge of the scene."*
*   **Prompt Scenario**: *"Create a level selector behavior script. When clicking numbers 1-3 on the keyboard, trigger SceneManager.LoadScene for level_1, level_2, or level_3 respectively."*
*   **Key Guardrail**: Always invoke `SceneManager.Initialize(Content)` in the application load block before calling `LoadScene` to avoid null reference exceptions.
