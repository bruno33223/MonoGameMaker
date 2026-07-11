# Sprite Sheet Animations

Mono GameMaker provides a built-in, mathematical animation system designed for uniform grid-based spritesheets. It automates frame counting, timer tracking, and texture source clipping (`SourceRect`) updates based on time.

---

## Animation API

To trigger or configure an animation, invoke the `PlayAnimation` method directly on a `GameEntity`:

```csharp
public void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps)
```

### Parameters
*   **`frameWidth`** (int): Width of a single sprite frame in pixels.
*   **`frameHeight`** (int): Height of a single sprite frame in pixels.
*   **`startFrame`** (int): Starting index of the animation sequence (0-indexed, left-to-right, top-to-bottom).
*   **`endFrame`** (int): Ending index of the animation loop.
*   **`fps`** (float): Frames per second to advance (controls animation speed).

---

## How Grid Rendering Works

When `PlayAnimation` is active:
1.  **Columns Calculation**:
    The system calculates columns based on texture size: `columns = Texture.Width / frameWidth`.
2.  **Coordinates Indexing**:
    It advances the frame index over time and calculates the row and column coordinates:
    `col = frameIndex % columns`
    `row = frameIndex / columns`
3.  **Viewport Clipping (`SourceRect`)**:
    It updates the entity's `SourceRect` property:
    `SourceRect = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight)`
4.  **Sprite Drawing**:
    The `EntityManager.Draw` loop automatically renders only the clipped `SourceRect` section of the spritesheet instead of drawing the entire image.
5.  **Collision Synchronization**:
    The entity's bounding box (`Bounds`) automatically matches the animation frame width and height rather than the entire spritesheet size, making collisions accurate.

---

## Practical Example: 4-Directional Walk Animation Script

Below is a complete implementation of a player character walking script that updates animations based on direction:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class AnimatedPlayer : EntityBehavior
    {
        private float _speed = 180f;

        public override void Awake()
        {
            // Start playing idle animation on frame 0 of a 32x32 spritesheet
            Entity.PlayAnimation(32, 32, 0, 0, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyState = Keyboard.GetState();
            Vector2 velocity = Vector2.Zero;

            // Detect movement direction
            if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up))
            {
                velocity.Y = -1;
                // Play Walk Up animation (frames 12 to 15 of spritesheet)
                Entity.PlayAnimation(32, 32, 12, 15, 8f);
            }
            else if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down))
            {
                velocity.Y = 1;
                // Play Walk Down animation (frames 0 to 3 of spritesheet)
                Entity.PlayAnimation(32, 32, 0, 3, 8f);
            }
            else if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
            {
                velocity.X = -1;
                // Play Walk Left animation (frames 4 to 7 of spritesheet)
                Entity.PlayAnimation(32, 32, 4, 7, 8f);
            }
            else if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
            {
                velocity.X = 1;
                // Play Walk Right animation (frames 8 to 11 of spritesheet)
                Entity.PlayAnimation(32, 32, 8, 11, 8f);
            }

            // Apply movement
            if (velocity != Vector2.Zero)
            {
                velocity.Normalize();
                Entity.Position += velocity * _speed * dt;
            }
            else
            {
                // Play idle face-down frame if stationary
                Entity.PlayAnimation(32, 32, 0, 0, 0f);
            }
        }
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to implement sprite animations:

*   **Prompt Scenario**: *"Write an Update loop that triggers a 4-frame walk cycle at 12 FPS when velocity is non-zero, and falls back to frame 0 at 0 FPS when stationary."*
*   **Prompt Scenario**: *"Trigger a temporary explosion animation (frames 8 to 15 at 15 FPS) when the projectile collides with an obstacle."*
*   **Key Guardrail**: Call `PlayAnimation` on every frame if movement continues; the system automatically ignores repetitive parameters to avoid resetting the active animation timer.
