# ImGui UI Space Manual

Mono GameMaker integrates `ImGui.NET` natively within the gameplay simulation and execution pipeline. This manual explains how the context isolation works, the drawing sequence, and how to write scripts that leverage ImGui for gameplay and diagnostic UIs.

---

## Architecture & Rendering Isolation

ImGui modifies the low-level graphics device state (such as rasterizer states, scissor regions, and blend factors). To avoid corrupted rendering states or texture bleeding on your 2D SpriteBatch elements, the execution cycle enforces strict **state isolation**:

1. **2D World Space Rendering**:
   `SpriteBatch.Begin()` is called to draw game entities (e.g., players, colliders, background textures). Once done, `SpriteBatch.End()` is called.
2. **2D Screen Space HUD (Traditional)**:
   A standard SpriteBatch drawing pass runs to render HUD items (e.g. hearts, static text) using `DrawUI(SpriteBatch)`. Once done, `SpriteBatch.End()` is called.
3. **ImGui UI Space (Immediate)**:
   The ImGui frame is updated and rendered *after* all SpriteBatch operations are closed. The standalone engine invokes `_imguiRenderer.AfterLayout()` to flush immediate mode GUI commands to the viewport on top of everything else.

---

## Overriding DrawUI for ImGui

To draw using ImGui, do **not** override the `DrawUI(SpriteBatch spriteBatch)` method (which is intended for standard 2D rendering). Instead, override the parameterless `DrawUI()` method:

```csharp
namespace MonoGameMaker.Runtime
{
    public abstract class EntityBehavior
    {
        // For standard 2D SpriteBatch drawing
        public virtual void DrawUI(SpriteBatch spriteBatch);

        // For direct ImGui immediate-mode drawing
        public virtual void DrawUI();
    }
}
```

---

## Practical Script Example (Inventory & Score HUD)

Below is a complete, compile-ready script implementing a dynamic HUD window using ImGui:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;
using ImGuiNET;

namespace MyGame.Scripts
{
    public class HUDController : EntityBehavior
    {
        private int _score;

        public override void Awake()
        {
            // Initialize score state
            GameState.Set("Score", 0);
        }

        public override void Update(GameTime gameTime)
        {
            // Simulate score accumulation
            if (Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                _score = GameState.Get<int>("Score") + 1;
                GameState.Set("Score", _score);
            }
        }

        public override void DrawUI()
        {
            // Create a custom ImGui window
            ImGui.Begin("Game Dashboard");

            // Display stats
            ImGui.Text("Score: " + _score);
            
            // Interaction button
            if (ImGui.Button("Reset Score"))
            {
                _score = 0;
                GameState.Set("Score", 0);
            }

            ImGui.End();
        }
    }
}
```

---

## Input Focus, Hover Logic & Spatial Containment

When the simulation is active inside the IDE editor viewport:
* **Viewport Containment**: All windows created by game scripts are natively enclausured within a transparent ImGui child window (`GameRuntimeViewportZone`). The boundaries of this child window match the exact dimensions of the active viewport. Attempts to drag simulated UI windows outside the boundaries will clip them visually.
* **Input Routing**: Clicks and key presses directed at game-created ImGui windows are captured by ImGui (`ImGui.GetIO().WantCaptureMouse` is `true`), which automatically suppresses mouse picking and object selection inside the IDE canvas, preventing accidental prefab alterations.
* **Layout Safety**: Window dragging and resizing inside the game viewport are restricted from shifting or docking authoring windows (like the Properties/Project Explorer panels), ensuring full interface isolation.
* **ASCII Font Prohibition**: Do not generate manual character bitmaps. Always draw strings via `TextRenderer.Draw()` or `ImGui.Text()`.

