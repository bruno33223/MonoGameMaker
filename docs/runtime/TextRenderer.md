# TextRenderer API Manual

The `TextRenderer` is a static, global utility within the Mono GameMaker runtime designed to draw text strings. It eliminates the need for generating manual ASCII pixel characters or custom bitmap dictionaries in behavior scripts.

---

## Method Signature

```csharp
namespace MonoGameMaker.Runtime
{
    public static class TextRenderer
    {
        /// <summary>
        /// Renders a text string on the screen (UI Space) at the specified coordinates.
        /// </summary>
        /// <param name="text">The string content to display.</param>
        /// <param name="position">Screen Space X and Y position coordinates.</param>
        /// <param name="color">The tint color of the text.</param>
        public static void Draw(string text, Vector2 position, Color color);
    }
}
```

---

## How Fonts Work in Mono GameMaker

The `TextRenderer` compiles and resolves fonts dynamically through the MonoGame Content Pipeline:

1. **Scaffold Font**:
   During project creation, a default font template (`default.spritefont`) is created under the `Content/Fonts/` folder.
2. **Standard MGCB Compilation**:
   This `.spritefont` file is registered inside `Content/Content.mgcb` and compiled to a `.xnb` format binary during the build phase.
3. **Runtime Loading & Fallbacks**:
   - The engine attempts to load `Fonts/default` or `default` from the Content pipeline.
   - **Fallback**: If the default font file is missing, `TextRenderer` automatically switches to draw solid blocks per character to provide visual debug feedback rather than throwing a `ContentLoadException` and crashing.

---

## Customizing Aligned Text (Centering / Custom Layouts)

Although `TextRenderer` draws from the top-left coordinate of the text string by default, you can measure text bounds for custom alignment.

### Centering a text string:

```csharp
public override void DrawUI(SpriteBatch spriteBatch)
{
    string scoreText = "Score: " + GameState.Get<int>("Score");
    
    // Default size calculation fallback helper
    Vector2 textSize = new Vector2(scoreText.Length * 8, 12); 
    
    // If you have a custom SpriteFont, you can use MeasureString:
    // Vector2 textSize = myFont.MeasureString(scoreText);

    // Center text on the 1280x720 scene viewport:
    Vector2 position = new Vector2(640 - (textSize.X / 2), 360 - (textSize.Y / 2));
    TextRenderer.Draw(scoreText, position, Color.White);
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to create scoreboards, HUDs, or text elements:

* **Prompt Scenario**: *"Create a scoreboard in the top-right corner showing current lives."*
* **Key Guardrail**: Never implement a `CharPatterns` dictionary or manual drawing loops. Always invoke `TextRenderer.Draw` inside the `DrawUI` method of the controller entity.
