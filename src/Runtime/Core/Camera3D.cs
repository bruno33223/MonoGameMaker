using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Represents the 2D bounding rectangle of the camera's visible gameplay area on a world Z plane.
    /// </summary>
    public readonly struct WorldBounds2D
    {
        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
        public float Width => Right - Left;
        public float Height => Top - Bottom;

        public WorldBounds2D(float left, float right, float bottom, float top)
        {
            Left = MathF.Min(left, right);
            Right = MathF.Max(left, right);
            Bottom = MathF.Min(bottom, top);
            Top = MathF.Max(bottom, top);
        }

        public bool Contains(Vector2 point) => point.X >= Left && point.X <= Right && point.Y >= Bottom && point.Y <= Top;
        public bool Contains(float x, float y) => x >= Left && x <= Right && y >= Bottom && y <= Top;
        public Vector2 Center => new Vector2((Left + Right) * 0.5f, (Top + Bottom) * 0.5f);

        public override string ToString() => $"[Left: {Left:F2}, Right: {Right:F2}, Bottom: {Bottom:F2}, Top: {Top:F2}, Width: {Width:F2}, Height: {Height:F2}]";
    }

    /// <summary>
    /// Perspective 3D and 2.5D Camera system featuring planar Trauma-based Screen Shake rumble, 
    /// exact screen-to-world unprojection raycasting, virtual 2D world viewport boundaries, and target tracking.
    /// </summary>
    public class Camera3D
    {
        private Vector3 _position = new Vector3(0f, 0f, 100f);
        private Vector3 _target = Vector3.Zero;
        private Vector3 _up = Vector3.Up;
        private float _fieldOfView = MathHelper.ToRadians(55f);
        private float _aspectRatio = 16f / 9f;
        private float _nearPlane = 0.1f;
        private float _farPlane = 1000f;
        private float _trauma = 0f;
        private float _shakeTimer = 0f;

        #region Camera Properties

        /// <summary>
        /// Gets or sets the base position of the camera in 3D world space.
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (_trauma <= 0.0001f)
                {
                    EffectivePosition = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the base look-at target point in 3D world space.
        /// </summary>
        public Vector3 Target
        {
            get => _target;
            set
            {
                _target = value;
                if (_trauma <= 0.0001f)
                {
                    EffectiveTarget = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the camera's up vector (defaults to Vector3.Up).
        /// </summary>
        public Vector3 Up
        {
            get => _up;
            set
            {
                _up = value;
                if (_trauma <= 0.0001f)
                {
                    EffectiveUp = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the vertical field of view in radians (default is 55 degrees).
        /// </summary>
        public float FieldOfView
        {
            get => _fieldOfView;
            set => _fieldOfView = Math.Max(0.001f, Math.Min(MathHelper.Pi - 0.001f, value));
        }

        /// <summary>
        /// Gets or sets the viewport aspect ratio (width / height, default 16:9).
        /// </summary>
        public float AspectRatio
        {
            get => _aspectRatio;
            set => _aspectRatio = Math.Max(0.001f, value);
        }

        /// <summary>
        /// Gets or sets the near clipping plane distance (defaults to 0.1f).
        /// </summary>
        public float NearPlane
        {
            get => _nearPlane;
            set => _nearPlane = Math.Max(0.0001f, value);
        }

        /// <summary>
        /// Gets or sets the far clipping plane distance (defaults to 1000f).
        /// </summary>
        public float FarPlane
        {
            get => _farPlane;
            set => _farPlane = Math.Max(_nearPlane + 0.001f, value);
        }

        /// <summary>
        /// Optional explicit viewport instance. If null, a fallback viewport matching AspectRatio is used.
        /// </summary>
        public Viewport? Viewport { get; set; }

        /// <summary>
        /// Gets the effective camera position after applying planar trauma screen shake offsets.
        /// </summary>
        public Vector3 EffectivePosition { get; private set; } = new Vector3(0f, 0f, 100f);

        /// <summary>
        /// Gets the effective camera target after applying planar trauma screen shake offsets.
        /// </summary>
        public Vector3 EffectiveTarget { get; private set; } = Vector3.Zero;

        /// <summary>
        /// Gets the effective camera up vector after applying trauma screen shake rotations.
        /// </summary>
        public Vector3 EffectiveUp { get; private set; } = Vector3.Up;

        #endregion

        #region Screen Shake & Trauma Properties

        /// <summary>
        /// Gets or sets the current camera trauma level clamped in [0, 1].
        /// </summary>
        public float Trauma
        {
            get => _trauma;
            set => _trauma = MathHelper.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Trauma decay rate per second (default is 2.8f).
        /// </summary>
        public float TraumaDecay { get; set; } = 2.8f;

        /// <summary>
        /// Maximum translation displacement in world units when trauma is at 1.0 (default is 3.0f).
        /// </summary>
        public float MaxShakeTranslation { get; set; } = 3.0f;

        /// <summary>
        /// Maximum rotational shake in radians when trauma is at 1.0 (default is 0f for zero angular tipping).
        /// </summary>
        public float MaxShakeRotation { get; set; } = 0f;

        /// <summary>
        /// Frequency of screen shake oscillations in Hz (default is 25f).
        /// </summary>
        public float ShakeFrequency { get; set; } = 25f;

        #endregion

        #region World Bounds Properties (at Z = 0)

        /// <summary>
        /// Gets the active 2D world gameplay bounds at Z = 0.
        /// </summary>
        public WorldBounds2D WorldBounds => GetViewportWorldBounds(0f);

        /// <summary>
        /// Leftmost visible world X coordinate at Z = 0.
        /// </summary>
        public float LeftBound => GetViewportWorldBounds(0f).Left;

        /// <summary>
        /// Rightmost visible world X coordinate at Z = 0.
        /// </summary>
        public float RightBound => GetViewportWorldBounds(0f).Right;

        /// <summary>
        /// Topmost visible world Y coordinate at Z = 0.
        /// </summary>
        public float TopBound => GetViewportWorldBounds(0f).Top;

        /// <summary>
        /// Bottommost visible world Y coordinate at Z = 0.
        /// </summary>
        public float BottomBound => GetViewportWorldBounds(0f).Bottom;

        /// <summary>
        /// Total visible world width at Z = 0.
        /// </summary>
        public float WorldWidth => GetViewportWorldBounds(0f).Width;

        /// <summary>
        /// Total visible world height at Z = 0.
        /// </summary>
        public float WorldHeight => GetViewportWorldBounds(0f).Height;

        #endregion

        #region Transformation Matrices

        /// <summary>
        /// Gets the active view matrix computed from effective (shaken) position, target, and up vectors.
        /// </summary>
        public Matrix ViewMatrix => Matrix.CreateLookAt(EffectivePosition, EffectiveTarget, EffectiveUp);

        /// <summary>
        /// Gets the perspective projection matrix based on FieldOfView, AspectRatio, NearPlane, and FarPlane.
        /// </summary>
        public Matrix ProjectionMatrix => Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);

        /// <summary>
        /// Gets the combined View * Projection matrix for vertex transformation and shader parameters.
        /// </summary>
        public Matrix ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

        /// <summary>
        /// Gets the un-shaken base view matrix computed from base Position, Target, and Up vectors.
        /// </summary>
        public Matrix BaseViewMatrix => Matrix.CreateLookAt(Position, Target, Up);

        /// <summary>
        /// Gets the combined un-shaken BaseViewMatrix * ProjectionMatrix.
        /// </summary>
        public Matrix BaseViewProjectionMatrix => BaseViewMatrix * ProjectionMatrix;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="Camera3D"/> with default perspective parameters.
        /// </summary>
        public Camera3D()
        {
            EffectivePosition = Position;
            EffectiveTarget = Target;
            EffectiveUp = Up;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Camera3D"/> with a specified aspect ratio.
        /// </summary>
        /// <param name="aspectRatio">Viewport aspect ratio (width / height).</param>
        public Camera3D(float aspectRatio) : this()
        {
            AspectRatio = aspectRatio;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Camera3D"/> with a specified viewport.
        /// </summary>
        /// <param name="viewport">The active graphics viewport.</param>
        public Camera3D(Viewport viewport) : this()
        {
            Viewport = viewport;
            AspectRatio = viewport.AspectRatio;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Camera3D"/> with custom position and target.
        /// </summary>
        public Camera3D(Vector3 position, Vector3 target, float aspectRatio = 16f / 9f, float fieldOfView = 0.959931f, float nearPlane = 0.1f, float farPlane = 1000f)
        {
            _position = position;
            _target = target;
            _aspectRatio = aspectRatio;
            _fieldOfView = fieldOfView;
            _nearPlane = nearPlane;
            _farPlane = farPlane;
            EffectivePosition = position;
            EffectiveTarget = target;
            EffectiveUp = Up;
        }

        #endregion

        #region Trauma & Shake Methods

        /// <summary>
        /// Adds trauma to the camera, clamped between 0 and 1.
        /// </summary>
        /// <param name="amount">The amount of trauma to add (e.g., 0.3 for a small hit, 0.7 for an explosion).</param>
        public void AddTrauma(float amount)
        {
            Trauma = MathHelper.Clamp(Trauma + amount, 0f, 1f);
        }

        /// <summary>
        /// Immediately clears all active trauma and resets camera displacements to zero.
        /// </summary>
        public void ResetShake()
        {
            Trauma = 0f;
            _shakeTimer = 0f;
            EffectivePosition = Position;
            EffectiveTarget = Target;
            EffectiveUp = Up;
        }

        /// <summary>
        /// Updates trauma decay and calculates screen shake offsets for the current frame.
        /// </summary>
        /// <param name="gameTime">Snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            if (gameTime == null) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Update(dt);
        }

        /// <summary>
        /// Updates trauma decay and calculates screen shake offsets for a specific delta time in seconds.
        /// Screen shake uses planar translation rumble with zero angular tipping by default.
        /// </summary>
        /// <param name="dt">Elapsed delta time in seconds.</param>
        public void Update(float dt)
        {
            if (dt < 0f) return;

            if (_trauma > 0f)
            {
                _trauma = MathF.Max(0f, _trauma - (TraumaDecay * dt));
                _shakeTimer += dt * ShakeFrequency;
            }
            else
            {
                _shakeTimer = 0f;
            }

            // Non-linear trauma response: displacement is proportional to Trauma^2 for punchy impacts
            float shake = _trauma * _trauma;

            if (shake > 0.00001f)
            {
                // Multi-frequency harmonic pseudo-noise for translation rumble
                float transX = (MathF.Sin(_shakeTimer * 1.0f) + (MathF.Sin(_shakeTimer * 2.3f) * 0.45f)) * (MaxShakeTranslation * shake);
                float transY = (MathF.Cos(_shakeTimer * 1.2f) + (MathF.Sin(_shakeTimer * 2.7f) * 0.45f)) * (MaxShakeTranslation * shake);
                float transZ = (MathF.Sin(_shakeTimer * 0.8f) + (MathF.Cos(_shakeTimer * 1.9f) * 0.45f)) * (MaxShakeTranslation * 0.2f * shake);

                // Calculate camera coordinate frame basis vectors
                Vector3 forward = _target - _position;
                float dist = forward.Length();
                if (dist < 0.0001f)
                {
                    forward = -Vector3.UnitZ;
                    dist = 1f;
                }

                Vector3 forwardNorm = forward / dist;
                Vector3 right = Vector3.Cross(forwardNorm, _up);
                if (right.LengthSquared() < 0.0001f)
                {
                    right = Vector3.UnitX;
                }
                else
                {
                    right.Normalize();
                }

                Vector3 actualUp = Vector3.Normalize(Vector3.Cross(right, forwardNorm));

                // Planar translation displacement
                Vector3 translationOffset = (right * transX) + (actualUp * transY) + (forwardNorm * transZ);
                EffectivePosition = _position + translationOffset;

                // Rotational angular shake (disabled by default with MaxShakeRotation = 0f to prevent 3D disorienting tilt)
                if (MaxShakeRotation > 0.0001f)
                {
                    float pitch = (MathF.Cos(_shakeTimer * 1.3f) + (MathF.Sin(_shakeTimer * 2.5f) * 0.4f)) * (MaxShakeRotation * shake);
                    float yaw = (MathF.Sin(_shakeTimer * 1.1f) + (MathF.Cos(_shakeTimer * 2.1f) * 0.4f)) * (MaxShakeRotation * shake);
                    float roll = (MathF.Sin(_shakeTimer * 1.7f) + (MathF.Cos(_shakeTimer * 3.1f) * 0.4f)) * (MaxShakeRotation * shake);

                    Matrix shakeRotation = Matrix.CreateFromAxisAngle(right, pitch) *
                                           Matrix.CreateFromAxisAngle(actualUp, yaw) *
                                           Matrix.CreateFromAxisAngle(forwardNorm, roll);

                    Vector3 shakenForward = Vector3.TransformNormal(forwardNorm, shakeRotation);
                    Vector3 shakenUp = Vector3.TransformNormal(actualUp, shakeRotation);

                    EffectiveTarget = EffectivePosition + (shakenForward * dist);
                    EffectiveUp = shakenUp;
                }
                else
                {
                    // Clean planar rumble: maintain exact look-direction and stable Up vector
                    EffectiveTarget = EffectivePosition + forward;
                    EffectiveUp = _up;
                }
            }
            else
            {
                EffectivePosition = _position;
                EffectiveTarget = _target;
                EffectiveUp = _up;
            }
        }

        #endregion

        #region Navigation & Tracking

        /// <summary>
        /// Centers the camera on a target position with a fixed camera offset.
        /// </summary>
        /// <param name="target">The world position to look at.</param>
        /// <param name="offset">The offset vector from the target to the camera position.</param>
        public void LookAt(Vector3 target, Vector3 offset)
        {
            Target = target;
            Position = target + offset;
            if (_trauma <= 0.0001f)
            {
                EffectivePosition = Position;
                EffectiveTarget = Target;
                EffectiveUp = Up;
            }
        }

        /// <summary>
        /// Rotates the camera to look at a new target while maintaining current relative distance and orientation.
        /// </summary>
        /// <param name="target">The new world position to look at.</param>
        public void LookAt(Vector3 target)
        {
            Vector3 offset = Position - Target;
            Target = target;
            Position = target + offset;
            if (_trauma <= 0.0001f)
            {
                EffectivePosition = Position;
                EffectiveTarget = Target;
                EffectiveUp = Up;
            }
        }

        #endregion

        #region Raycasting, Unprojection & Bounds Calculation

        /// <summary>
        /// Calculates the visible 2D world boundaries on a specified Z plane (default Z = 0).
        /// </summary>
        /// <param name="zPlane">Target world Z plane depth.</param>
        /// <param name="viewport">Optional viewport. If null, active Viewport or fallback is used.</param>
        /// <returns>WorldBounds2D containing Left, Right, Top, Bottom, Width, Height.</returns>
        public WorldBounds2D GetViewportWorldBounds(float zPlane = 0f, Viewport? viewport = null)
        {
            Viewport vp = viewport ?? Viewport ?? GetFallbackViewport();

            Vector3 pTL = ScreenToWorldPlane(new Vector2(0, 0), zPlane, vp);
            Vector3 pTR = ScreenToWorldPlane(new Vector2(vp.Width, 0), zPlane, vp);
            Vector3 pBL = ScreenToWorldPlane(new Vector2(0, vp.Height), zPlane, vp);
            Vector3 pBR = ScreenToWorldPlane(new Vector2(vp.Width, vp.Height), zPlane, vp);

            float minX = MathF.Min(MathF.Min(pTL.X, pTR.X), MathF.Min(pBL.X, pBR.X));
            float maxX = MathF.Max(MathF.Max(pTL.X, pTR.X), MathF.Max(pBL.X, pBR.X));
            float minY = MathF.Min(MathF.Min(pTL.Y, pTR.Y), MathF.Min(pBL.Y, pBR.Y));
            float maxY = MathF.Max(MathF.Max(pTL.Y, pTR.Y), MathF.Max(pBL.Y, pBR.Y));

            return new WorldBounds2D(minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Unprojects a 2D screen coordinate onto a horizontal/vertical 3D plane located at Z = <paramref name="zPlane"/> using exact ray-plane intersection.
        /// </summary>
        /// <param name="screenPos">Screen pixel coordinate (e.g. mouse position).</param>
        /// <param name="zPlane">The Z depth of the target plane in world space (default is 0).</param>
        /// <param name="viewport">Optional viewport. If null, active Viewport or fallback matching AspectRatio is used.</param>
        /// <returns>The 3D intersection point in world space.</returns>
        public Vector3 ScreenToWorldPlane(Vector2 screenPos, float zPlane = 0f, Viewport? viewport = null)
        {
            Viewport vp = viewport ?? Viewport ?? GetFallbackViewport();

            Vector3 nearSource = new Vector3(screenPos.X, screenPos.Y, 0f);
            Vector3 farSource = new Vector3(screenPos.X, screenPos.Y, 1f);

            Vector3 nearPoint = vp.Unproject(nearSource, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 farPoint = vp.Unproject(farSource, ProjectionMatrix, ViewMatrix, Matrix.Identity);

            Vector3 rayDir = farPoint - nearPoint;
            float dirLen = rayDir.Length();
            if (dirLen > 1e-6f)
            {
                rayDir /= dirLen;
            }
            else
            {
                return new Vector3(nearPoint.X, nearPoint.Y, zPlane);
            }

            // Ray equation: P = nearPoint + t * rayDir
            // Solve P.Z = zPlane => nearPoint.Z + t * rayDir.Z = zPlane => t = (zPlane - nearPoint.Z) / rayDir.Z
            if (MathF.Abs(rayDir.Z) < 1e-6f)
            {
                // Ray is parallel to the Z plane
                return new Vector3(nearPoint.X, nearPoint.Y, zPlane);
            }

            float t = (zPlane - nearPoint.Z) / rayDir.Z;
            Vector3 hitPoint = nearPoint + (rayDir * t);
            hitPoint.Z = zPlane; // Clamp exact plane depth
            return hitPoint;
        }

        /// <summary>
        /// Unprojects a 2D screen coordinate onto a plane at Z = <paramref name="zPlane"/> and returns the 2D (X, Y) world coordinates.
        /// </summary>
        /// <param name="screenPos">Screen pixel coordinate.</param>
        /// <param name="zPlane">The Z depth of the target plane in world space.</param>
        /// <param name="viewport">Optional viewport.</param>
        /// <returns>A Vector2 representing the (X, Y) world position on the plane.</returns>
        public Vector2 ScreenToWorldPlane2D(Vector2 screenPos, float zPlane = 0f, Viewport? viewport = null)
        {
            Vector3 world3D = ScreenToWorldPlane(screenPos, zPlane, viewport);
            return new Vector2(world3D.X, world3D.Y);
        }

        /// <summary>
        /// Constructs a 3D ray from the camera position through a 2D screen coordinate.
        /// </summary>
        /// <param name="screenPos">Screen pixel coordinate.</param>
        /// <param name="viewport">Optional viewport.</param>
        /// <returns>A world-space <see cref="Ray"/>.</returns>
        public Ray ScreenPointToRay(Vector2 screenPos, Viewport? viewport = null)
        {
            Viewport vp = viewport ?? Viewport ?? GetFallbackViewport();
            Vector3 nearSource = new Vector3(screenPos.X, screenPos.Y, 0f);
            Vector3 farSource = new Vector3(screenPos.X, screenPos.Y, 1f);

            Vector3 nearPoint = vp.Unproject(nearSource, ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 farPoint = vp.Unproject(farSource, ProjectionMatrix, ViewMatrix, Matrix.Identity);

            Vector3 rayDir = farPoint - nearPoint;
            if (rayDir.LengthSquared() > 1e-12f)
            {
                rayDir.Normalize();
            }
            else
            {
                rayDir = -Vector3.UnitZ;
            }

            return new Ray(nearPoint, rayDir);
        }

        /// <summary>
        /// Intersects a 2D screen ray with an arbitrary 3D <see cref="Plane"/>.
        /// </summary>
        /// <param name="screenPos">Screen pixel coordinate.</param>
        /// <param name="plane">Target 3D plane.</param>
        /// <param name="viewport">Optional viewport.</param>
        /// <returns>The intersection point in world space, or null if ray is parallel or pointing away.</returns>
        public Vector3? ScreenToPlane(Vector2 screenPos, Plane plane, Viewport? viewport = null)
        {
            Ray ray = ScreenPointToRay(screenPos, viewport);
            float? distance = ray.Intersects(plane);
            if (distance.HasValue)
            {
                return ray.Position + (ray.Direction * distance.Value);
            }
            return null;
        }

        /// <summary>
        /// Projects a 3D world position into 2D screen viewport coordinates.
        /// </summary>
        /// <param name="worldPos">The 3D point in world space.</param>
        /// <param name="viewport">Optional viewport.</param>
        /// <returns>Screen position in pixels with Z containing depth (0 = near, 1 = far).</returns>
        public Vector3 WorldToScreen(Vector3 worldPos, Viewport? viewport = null)
        {
            Viewport vp = viewport ?? Viewport ?? GetFallbackViewport();
            return vp.Project(worldPos, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        }

        /// <summary>
        /// Projects a 3D world position into 2D screen pixel coordinates (X, Y).
        /// </summary>
        /// <param name="worldPos">The 3D point in world space.</param>
        /// <param name="viewport">Optional viewport.</param>
        /// <returns>A Vector2 representing the screen coordinates in pixels.</returns>
        public Vector2 WorldToScreen2D(Vector3 worldPos, Viewport? viewport = null)
        {
            Vector3 screen3D = WorldToScreen(worldPos, viewport);
            return new Vector2(screen3D.X, screen3D.Y);
        }

        private Viewport GetFallbackViewport()
        {
            int height = 1080;
            int width = Math.Max(1, (int)MathF.Round(height * _aspectRatio));
            return new Viewport(0, 0, width, height);
        }

        #endregion
    }
}
