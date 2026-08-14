# `Camera3D`

The `Camera3D` class provides a high-precision perspective camera system tailored for 3D and 2.5D game development in Mono GameMaker. It encapsulates view and projection matrix computations, a non-linear trauma-based screen shake model with multi-frequency harmonic oscillations, screen-to-world unprojection raycasting for mouse aiming, and target tracking.

---

## Complete API Signatures

```csharp
namespace MonoGameMaker.Runtime.Core
{
    public class Camera3D
    {
        // Properties
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }

        public Vector3 EffectivePosition { get; }
        public Vector3 EffectiveTarget { get; }
        public Vector3 EffectiveUp { get; }

        // Matrices
        public Matrix ViewMatrix { get; }
        public Matrix ProjectionMatrix { get; }
        public Matrix ViewProjectionMatrix { get; }
        public Matrix BaseViewMatrix { get; }
        public Matrix BaseViewProjectionMatrix { get; }

        // Screen Shake / Trauma
        public float Trauma { get; set; }
        public float TraumaDecay { get; set; }
        public float MaxShakeTranslation { get; set; }
        public float MaxShakeRotation { get; set; }
        public float ShakeFrequency { get; set; }

        // Constructors
        public Camera3D();
        public Camera3D(float aspectRatio);
        public Camera3D(Viewport viewport);
        public Camera3D(Vector3 position, Vector3 target, float aspectRatio = 16f / 9f, float fieldOfView = 0.959931f, float nearPlane = 0.1f, float farPlane = 1000f);

        // Screen Shake Methods
        public void AddTrauma(float amount);
        public void ResetShake();
        public void Update(GameTime gameTime);
        public void Update(float dt);

        // Tracking & Navigation
        public void LookAt(Vector3 target, Vector3 offset);
        public void LookAt(Vector3 target);

        // Raycasting & Unprojection
        public Vector3 ScreenToWorldPlane(Vector2 screenPos, float planeZ = 0f, Viewport? viewport = null);
        public Vector2 ScreenToWorldPlane2D(Vector2 screenPos, float planeZ = 0f, Viewport? viewport = null);
        public Ray ScreenPointToRay(Vector2 screenPos, Viewport? viewport = null);
        public Vector3? ScreenToPlane(Vector2 screenPos, Plane plane, Viewport? viewport = null);
        public Vector3 WorldToScreen(Vector3 worldPos, Viewport? viewport = null);
        public Vector2 WorldToScreen2D(Vector3 worldPos, Viewport? viewport = null);
    }
}
```

---

## Architectural Overview & Math Breakdown

### 1. View & Perspective Projection Matrices

`Camera3D` generates right-handed view and perspective projection matrices compatible with MonoGame's shader pipeline (`BasicEffect`, custom HLSL shaders, and 3D particle systems):

$$\text{ViewMatrix} = \text{Matrix.CreateLookAt}(\text{EffectivePosition}, \text{EffectiveTarget}, \text{EffectiveUp})$$

$$\text{ProjectionMatrix} = \text{Matrix.CreatePerspectiveFieldOfView}(\text{FieldOfView}, \text{AspectRatio}, \text{NearPlane}, \text{FarPlane})$$

$$\text{ViewProjectionMatrix} = \text{ViewMatrix} \times \text{ProjectionMatrix}$$

### 2. Trauma-Based Screen Shake System

Rather than linear shake offsets which feel rigid and unnatural, `Camera3D` utilizes the **Trauma / Juice Model**:
- $\text{Trauma} \in [0.0, 1.0]$.
- $\text{Shake} = \text{Trauma}^2$ (quadratic falloff produces punchy initial hits that decay smoothly).
- Trauma decays linearly over time: $\text{Trauma} = \max(0, \text{Trauma} - \text{TraumaDecay} \times \Delta t)$.
- Multi-frequency harmonic sinusoidal oscillations drive translation along the camera's local coordinate axes ($\mathbf{Right}, \mathbf{Up}, \mathbf{Forward}$) and rotational perturbation ($\text{Pitch}, \text{Yaw}, \text{Roll}$).

```mermaid
graph LR
    Hit[AddTrauma(0.6)] --> Trauma[Trauma: 0.6]
    Trauma --> Quadratic[Shake = Trauma^2 = 0.36]
    Quadratic --> Harmonics[Multi-frequency Sin/Cos Oscillations]
    Harmonics --> Basis[Camera Local Basis: Right, Up, Forward]
    Basis --> Effective[EffectivePosition & EffectiveTarget]
    Effective --> ViewMat[ViewMatrix & ViewProjectionMatrix]
```

### 3. Screen-to-World Unprojection & Mouse Aiming

In 2.5D and 3D games, finding where the player clicked or is aiming on the gameplay plane (e.g. $Z = 0$) requires unprojecting 2D screen coordinates:

1. Unproject near plane point $(x, y, 0)$ and far plane point $(x, y, 1)$ via `Viewport.Unproject(..., ProjectionMatrix, ViewMatrix, Matrix.Identity)`.
2. Construct ray origin $\mathbf{O} = \text{nearPoint}$ and direction $\mathbf{D} = \text{Normalize}(\text{farPoint} - \text{nearPoint})$.
3. Intersect ray with target plane equation $Z = \text{planeZ}$:
   $$t = \frac{\text{planeZ} - \mathbf{O}_z}{\mathbf{D}_z}$$
4. Calculate world hit point $\mathbf{P} = \mathbf{O} + \mathbf{D} \times t$.

---

## Use Cases & AI Prompting Guidelines

| Scenario | Recommended Approach |
| :--- | :--- |
| **Top-down / Isometric 2.5D Action** | Use `Camera3D.LookAt(playerPos, new Vector3(0, -60, 120))` and `ScreenToWorldPlane(mousePos, planeZ: 0)` for cursor aiming. |
| **Explosions & Heavy Impacts** | Call `Camera3D.AddTrauma(0.7f)` during projectile explosions or player damage. The camera will shake violently and decay back to rest. |
| **Third-Person 3D Follow** | Update `Camera3D.Position` with smoothed spherical coordinates around the target and call `Camera3D.LookAt(target)`. |
| **Health Bars & World UI Overlays** | Use `Camera3D.WorldToScreen2D(entity.Position3D)` to project entity positions to screen coordinates for rendering UI elements via `SpriteBatch`. |

### AI Prompting Examples

* **Prompt**: *"Create a 2.5D top-down shooter controller where the camera follows the player at an offset of (0, -80, 150) and calculates the player's weapon aim vector towards the mouse cursor using `Camera3D.ScreenToWorldPlane2D`."*
* **Prompt**: *"Add screen shake to the camera whenever an enemy is defeated using `Camera3D.AddTrauma(0.4f)`."*

---

## Practical & Compilable Example

Below is a complete `EntityBehavior` demonstrating smooth camera tracking, trauma screen shake on impact, and mouse aiming on the $Z = 0$ gameplay plane:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime;
using MonoGameMaker.Runtime.Core;

namespace MyGame.Scripts
{
    public class Player3DController : EntityBehavior
    {
        public Camera3D Camera { get; private set; } = null!;

        private Vector3 _position = Vector3.Zero;
        private Vector3 _cameraOffset = new Vector3(0f, -70f, 130f);
        private float _moveSpeed = 200f;
        private float _aimAngle = 0f;

        public override void Awake()
        {
            // Initialize 3D perspective camera
            Camera = new Camera3D(16f / 9f)
            {
                FieldOfView = MathHelper.ToRadians(60f),
                NearPlane = 0.1f,
                FarPlane = 2000f,
                TraumaDecay = 1.8f,
                MaxShakeTranslation = 20f,
                MaxShakeRotation = 0.12f
            };

            Camera.LookAt(_position, _cameraOffset);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. Movement Input
            var kb = Keyboard.GetState();
            Vector3 moveDir = Vector3.Zero;
            if (kb.IsKeyDown(Keys.W)) moveDir.Y += 1f;
            if (kb.IsKeyDown(Keys.S)) moveDir.Y -= 1f;
            if (kb.IsKeyDown(Keys.A)) moveDir.X -= 1f;
            if (kb.IsKeyDown(Keys.D)) moveDir.X += 1f;

            if (moveDir.LengthSquared() > 0.001f)
            {
                moveDir.Normalize();
                _position += moveDir * _moveSpeed * dt;
            }

            // 2. Aiming at Mouse Cursor on Z = 0 plane
            var mouse = Mouse.GetState();
            Vector2 mouseScreen = new Vector2(mouse.X, mouse.Y);
            Vector3 aimWorldPos = Camera.ScreenToWorldPlane(mouseScreen, planeZ: 0f);

            Vector3 toAim = aimWorldPos - _position;
            _aimAngle = MathF.Atan2(toAim.Y, toAim.X);

            // 3. Trigger Explosion Shake on Space key
            if (kb.IsKeyDown(Keys.Space))
            {
                Camera.AddTrauma(0.8f);
            }

            // 4. Smooth Camera Following
            Vector3 targetCamPos = _position + _cameraOffset;
            Camera.Position = Vector3.Lerp(Camera.Position, targetCamPos, 8f * dt);
            Camera.Target = Vector3.Lerp(Camera.Target, _position, 8f * dt);

            // 5. Update Camera Screen Shake
            Camera.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // World Space rendering
        }

        public override void DrawUI(SpriteBatch spriteBatch)
        {
            // Project player position to screen coordinates for floating health bar
            Vector2 screenPos = Camera.WorldToScreen2D(_position);
            TextRenderer.Draw($"Pos: ({_position.X:F0}, {_position.Y:F0}) | Trauma: {Camera.Trauma:F2}", new Vector2(20, 20), Color.White);
        }
    }
}
```
