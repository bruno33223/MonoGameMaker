# Custom Fonts & Text Rendering Manual

This manual details the creation, configuration, compilation, and dynamic use of custom compiled fonts (`.spritefont` / `.xnb`) inside Mono GameMaker.

---

## 1. High-Level Description
The custom font system replaces static hardcoded font loaders with a data-driven pipeline. 
- Custom fonts are configured visually inside the IDE, which generates structured XML descriptions (`.spritefont`).
- The MonoGame Content Builder (MGCB) compiles them into binary asset files (`.xnb`) in the background.
- At runtime, `TextRenderer` dynamically loads all compiled fonts from the `Content/Fonts/` output directory into a cache. User scripts can then specify which font to render by passing its name as a parameter to the overloaded `Draw` method.

---

## 2. API Signatures

### TextRenderer API
Exposed globally under the `MonoGameMaker.Runtime` assembly:

```csharp
namespace MonoGameMaker.Runtime
{
    public static class TextRenderer
    {
        /// <summary>
        /// Draws a text string onto the screen space at the specified coordinates using the default font.
        /// </summary>
        public static void Draw(string text, Vector2 position, Color color);

        /// <summary>
        /// Draws a text string onto the screen space at the specified coordinates using a custom compiled font name.
        /// </summary>
        /// <param name="text">The string content to render.</param>
        /// <param name="position">Screen space coordinates.</param>
        /// <param name="color">Text tint color.</param>
        /// <param name="fontName">The registered compiled custom font name (e.g. "ScoreFont"). Defaults to "default".</param>
        public static void Draw(string text, Vector2 position, Color color, string fontName = "default");
    }
}
```

---

## 3. Use Cases (Context for AI Assistants)
- **Stylized HUD Overlay**: Use custom fonts with distinctive styles (e.g., retro pixel, futuristic) to render stats, inventory, or meters.
- **Large Banner Text**: Use a larger font size configuration (e.g. `24px` or `32px` Bold) for Game Over, Level Cleared, or Pause screens without manually scaling small default fonts (which leads to pixelation).
- **Localized / Custom Character Ranges**: Configure the character regions in the `.spritefont` XML description to support special symbols or unicode blocks.

---

## 4. Practical C# Compileable Example

The following script behavior shows how a UI controller handles drawing both standard messages and high-resolution scores using a custom compiled font named `"ScoreFont"`:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class GameHudController : EntityBehavior
    {
        private int _coinsCollected = 0;
        private bool _isGameOver = false;

        public override void Awake()
        {
            GlobalState.Log("[HUD] Script started. Loading fonts...");
        }

        public override void Update(GameTime gameTime)
        {
            // Sync values from persistent global game state
            _coinsCollected = GameState.Get<int>("Coins", 0);
            _isGameOver = GameState.Get<bool>("IsGameOver", false);
        }

        public override void DrawUI(SpriteBatch spriteBatch)
        {
            // 1. Draw health stats with fallback default font
            TextRenderer.Draw("HP: 100/100", new Vector2(20, 20), Color.LightGreen);

            // 2. Draw coins count with compiled custom font "ScoreFont"
            TextRenderer.Draw($"COINS: {_coinsCollected}", new Vector2(20, 50), Color.Gold, "ScoreFont");

            // 3. Draw Game Over screen in giant custom font "GameOverFont" if active
            if (_isGameOver)
            {
                TextRenderer.Draw("GAME OVER", new Vector2(400, 300), Color.Red, "GameOverFont");
                TextRenderer.Draw("Press F5 to restart", new Vector2(420, 360), Color.White);
            }
        }
    }
}
```
