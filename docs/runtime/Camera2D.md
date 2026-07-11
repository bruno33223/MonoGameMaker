# `Camera2D` (Static Class)

The `Camera2D` class calculates viewport translation offsets. It exposes a transformation matrix that adjusts rendering coordinates to follow target coordinates while clamping the viewport inside the boundaries of the active scene.

---

## API Signatures

```csharp
namespace MonoGameMaker.Runtime
{
    public static class Camera2D
    {
        public static Vector2 Position { get; set; }
        public static Matrix Transform { get; }

        public static void LookAt(Vector2 target, int viewportWidth, int viewportHeight);
    }
}
```

---

## Translation Matrix & Clamping Logic

### 1. `Transform` Matrix
The camera applies a 2D offset matrix to shift world space coordinates:
`Matrix.CreateTranslation(-Position.X, -Position.Y, 0)`
When passed into `SpriteBatch.Begin(..., transformMatrix: Camera2D.Transform)`, all drawings are rendered relative to the camera's viewport coordinate frame.

### 2. Clamping Bounds (`LookAt`)
When calling `LookAt(target, viewportWidth, viewportHeight)`, the camera centers the target position inside the viewport:
`targetX - (viewportWidth / 2f)`
`targetY - (viewportHeight / 2f)`
It queries `SceneManager.CurrentScene` to retrieve scene dimensions (`sceneWidth`, `sceneHeight`), and clamps the offset bounds:
*   **X range**: `0` to `sceneWidth - viewportWidth`
*   **Y range**: `0` to `sceneHeight - viewportHeight`

This prevents the camera from displaying void areas beyond the active scene grid.

---

## Practical Example: Smooth Follow Camera Script

Below is a complete implementation of a tracking camera script that smooths camera movements toward a player entity using linear interpolation (`Lerp`) while respecting scene limits:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime;

namespace MyGame.Scripts
{
    public class CameraFollower : IEntityScript
    {
        private GameEntity _entity;
        private GameEntity _playerTarget;
        private float _lerpSpeed = 5f; // Speed coefficient of camera smoothing

        public void Initialize(GameEntity entity, Dictionary<string, string> properties)
        {
            _entity = entity;
            if (properties.TryGetValue("Speed", out var speedStr))
            {
                float.TryParse(speedStr, out _lerpSpeed);
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Locate player entity dynamically if not cached
            if (_playerTarget == null)
            {
                foreach (var activeEnt in EntityManager.Entities)
                {
                    if (activeEnt.Tag == "Player")
                    {
                        _playerTarget = activeEnt;
                        break;
                    }
                }
            }

            if (_playerTarget != null)
            {
                // Target center coordinate
                Vector2 targetPos = _playerTarget.Position;

                // Center camera relative to 1280x720 viewport resolution
                float desiredX = targetPos.X - (1280 / 2f);
                float desiredY = targetPos.Y - (720 / 2f);

                // Query boundaries from active scene
                int sceneWidth = 1280;
                int sceneHeight = 720;
                if (SceneManager.CurrentScene != null)
                {
                    sceneWidth = SceneManager.CurrentScene.Width;
                    sceneHeight = SceneManager.CurrentScene.Height;
                }

                // Clamp limits
                float minX = 0f;
                float maxX = Math.Max(0f, sceneWidth - 1280);
                float minY = 0f;
                float maxY = Math.Max(0f, sceneHeight - 720);

                float clampedX = MathHelper.Clamp(desiredX, minX, maxX);
                float clampedY = MathHelper.Clamp(desiredY, minY, maxY);

                // Smooth interpolate camera position
                Vector2 smoothedPosition = new Vector2(
                    MathHelper.Lerp(Camera2D.Position.X, clampedX, _lerpSpeed * dt),
                    MathHelper.Lerp(Camera2D.Position.Y, clampedY, _lerpSpeed * dt)
                );

                Camera2D.Position = smoothedPosition;
            }
        }

        public void Draw(SpriteBatch spriteBatch) {}
        public void DrawUI(SpriteBatch spriteBatch) {}
    }
}
```

---

## AI Prompting & Context Guidelines

When directing an AI coding assistant to handle camera configurations or focus shifts:

*   **Prompt Scenario**: *"Write a Camera tracking script that directly centers on the player coordinates inside its Update loop using the Camera2D.LookAt method."*
*   **Prompt Scenario**: *"Create a script that applies a camera shake screen effect by modifying Camera2D.Position offsets with random values when an explosion event occurs."*
*   **Key Guardrail**: The scene dimensions must be configured correctly in `SceneManager.CurrentScene` (e.g. loaded via JSON) for automatic clamping inside `LookAt` to function correctly.
