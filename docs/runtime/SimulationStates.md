# Simulation States & Focus-Based Input Isolation Manual

This manual details the simulation state cycle, frame stepping APIs, and focus-based input isolation system used in Mono GameMaker.

---

## 1. High-Level Description
The simulation state cycle resolves the problem of separating editor authoring from active game simulation execution. It defines three key states:
*   `Edit`: The design-time environment. User scripts are not executed, physics are static, and entity positions are cached.
*   `Playing`: Active simulation mode. The game loop updates and user scripts run in real-time.
*   `Paused`: Simulation loop is suspended. Physics and animations freeze. Developers can trigger single-frame steps to inspect behavior frame by frame.

To prevent input conflicts (e.g. typing in the inspector triggering character movement), the engine intercepts inputs when the Game Viewport is not active:
*   **Viewport Focused**: Real OS inputs from the keyboard and mouse are translated to local viewport coordinates and forwarded to user scripts.
*   **Viewport Unfocused**: All inputs are muted (returned as empty/released states).

---

## 2. API Signatures

### Simulation Control State
Exposed under the `MonoGameMaker.Runtime` assembly via reflection or through global properties:

```csharp
namespace MonoGameMaker.Runtime
{
    public static class Keyboard
    {
        /// <summary>
        /// Retrieves the isolated/focused keyboard state for this frame.
        /// </summary>
        public static KeyboardState GetState();

        /// <summary>
        /// Updates the current shadow keyboard state.
        /// </summary>
        public static void SetState(KeyboardState state);
    }

    public static class Mouse
    {
        /// <summary>
        /// Retrieves the isolated/focused mouse state for this frame (translated to viewport coordinates).
        /// </summary>
        public static MouseState GetState();

        /// <summary>
        /// Updates the current shadow mouse state.
        /// </summary>
        public static void SetState(MouseState state);
    }
}
```

---

## 3. Use Cases (Context for AI Assistants)
*   **Preventing Editor-Sim Input Collision**: When typing a name for an entity in the Inspector window, user scripts shouldn't interpret keys like `W/A/S/D` as character movement. The focus-based input isolation mutes all game inputs automatically.
*   **Frame-by-Frame Debugging**: When debugging physical collisions, pause the simulation and click the **Step Frame** button to execute a single 1/60-second physics/script update cycle and inspect the results.

---

## 4. Practical C# Compileable Example

The following is a compileable script illustrating how user scripts safely read input and update logic. Thanks to shadow inputs, standard MonoGame APIs work as expected, but are automatically isolated and translated:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class PlayerController : EntityBehavior
    {
        private float _speed = 200f;

        public override void Awake()
        {
            if (Properties.TryGetValue("Speed", out string speedVal))
            {
                float.TryParse(speedVal, out _speed);
            }
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Reading shadowed/isolated keyboard state
            var keyState = Keyboard.GetState();
            Vector2 velocity = Vector2.Zero;

            if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up))
                velocity.Y = -1;
            if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down))
                velocity.Y = 1;
            if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
                velocity.X = -1;
            if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
                velocity.X = 1;

            if (velocity != Vector2.Zero)
            {
                velocity.Normalize();
                Entity.Position += velocity * _speed * dt;
            }

            // Reading shadowed/isolated mouse state (automatically translated to viewport coordinates)
            var mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // Move towards mouse position
                Vector2 target = new Vector2(mouseState.X, mouseState.Y);
                Vector2 dir = target - Entity.Position;
                if (dir.Length() > 5f)
                {
                    dir.Normalize();
                    Entity.Position += dir * _speed * dt;
                }
            }
        }
    }
}
```
