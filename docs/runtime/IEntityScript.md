# `IEntityScript` (Interface)

The `IEntityScript` interface is the core behavior contract in Mono GameMaker. Every gameplay script class must implement this interface to receive lifecycle events from the runtime game engine.

---

## Lifecycle API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public interface IEntityScript
    {
        void Initialize(GameEntity entity, Dictionary<string, string> properties);
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
        void DrawUI(SpriteBatch spriteBatch);
    }
}
```

### 1. `Initialize`
- **Execution**: Triggered once when the entity is instantiated and added to the scene.
- **Purpose**: Used to link the script instance to its parent `GameEntity` and load custom properties specified in the Scene JSON or Prefab configuration.

### 2. `Update`
- **Execution**: Invoked once per game tick during the logic update pass of `EntityManager.Update()`.
- **Purpose**: Handles input reading, movement, timers, and game physics/collisions.

### 3. `Draw`
- **Execution**: Invoked during the World Space rendering pass.
- **Purpose**: Renders visual components affected by the camera transform matrix (e.g. sprites, background elements, visual effects). If left blank, the engine automatically draws the entity's texture at its coordinate position.

### 4. `DrawUI`
- **Execution**: Invoked during the Screen Space rendering pass after all world drawings are complete.
- **Purpose**: Renders user interfaces, overlays, health bars, and overlay text that stay fixed on screen, ignoring camera translations.

---

## Practical Example: Blinking Score HUD Script

Below is a complete script demonstrating how to use `DrawUI` to render a flashing overlay HUD using game time to calculate alpha blinking:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class BlinkingScoreHud : IEntityScript
    {
        private GameEntity _entity;
        private float _blinkTimer = 0f;
        private bool _isVisible = true;
        private SpriteFont _font; // Dynamically loaded font

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
            // Load font from the project ContentManager
            if (EntityManager.Content != null)
            {
                try
                {
                    _font = EntityManager.Content.Load<SpriteFont>("Fonts/ScoreFont");
                }
                catch
                {
                    // Fallback or ignore if font asset is missing
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            // Update blinking timing
            _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_blinkTimer >= 0.5f) // Blink every 500ms
            {
                _isVisible = !_isVisible;
                _blinkTimer = 0f;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Keep empty to let the engine render default texture in world space
        }

        public void DrawUI(SpriteBatch spriteBatch)
        {
            if (_font == null) return;

            // Fetch score from global GameState
            int currentScore = GameState.Get<int>("Score", 0);
            string scoreText = $"SCORE: {currentScore:D6}";

            Color textColor = _isVisible ? Color.Yellow : Color.Yellow * 0.5f;

            // Draw score at fixed screen coordinates (top-right corner)
            spriteBatch.DrawString(_font, scoreText, new Vector2(20, 20), textColor);
        }
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to create script behaviors using `IEntityScript`:

*   **Prompt Scenario**: *"Create an IEntityScript class that manages player movement. Initialize it with a Speed parameter, update coordinates inside the Update loop based on Keyboard input, and let the engine default to drawing the entity texture."*
*   **Prompt Scenario**: *"Write an entity behavior that implements a floating health bar. Draw the player sprite in the World Space Draw method, and paint the overlay health bar in the Screen Space DrawUI method so it does not skew with camera movements."*
*   **Key Guardrail**: Do not declare custom `Awake()` or `Start()` methods in the script class. All script instances receive context parameters exclusively inside `Initialize`.
