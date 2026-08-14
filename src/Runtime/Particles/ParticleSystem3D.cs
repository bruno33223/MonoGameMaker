using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Core;

namespace MonoGameMaker.Runtime.Particles
{
    /// <summary>
    /// Represents an individual 3D particle state in world space.
    /// Memory layout is tightly packed to avoid GC allocations during lifecycle updates.
    /// </summary>
    public struct Particle3D
    {
        /// <summary>
        /// 3D position of the particle in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 3D linear velocity vector (units per second).
        /// </summary>
        public Vector3 Velocity;

        /// <summary>
        /// 3D constant linear acceleration (e.g. gravity, wind, propulsion).
        /// </summary>
        public Vector3 Acceleration;

        /// <summary>
        /// Color tint at spawn time (t = 0).
        /// </summary>
        public Color Color;

        /// <summary>
        /// Color tint at death time (t = MaxLife).
        /// </summary>
        public Color EndColor;

        /// <summary>
        /// Billboard size/diameter in world units at spawn time.
        /// </summary>
        public float Size;

        /// <summary>
        /// Billboard size/diameter in world units at death time.
        /// </summary>
        public float EndSize;

        /// <summary>
        /// Current planar rotation angle in radians around the billboard normal.
        /// </summary>
        public float Rotation;

        /// <summary>
        /// Angular velocity in radians per second.
        /// </summary>
        public float RotationSpeed;

        /// <summary>
        /// Remaining lifetime in seconds.
        /// </summary>
        public float Life;

        /// <summary>
        /// Initial total lifetime in seconds.
        /// </summary>
        public float MaxLife;

        /// <summary>
        /// Indicates whether this particle slot is currently active and rendered.
        /// </summary>
        public bool Active;

        /// <summary>
        /// Gets the normalized progression of particle life [0.0 = spawn, 1.0 = death].
        /// </summary>
        public readonly float NormalizedProgress => MaxLife > 0.0001f ? MathHelper.Clamp(1f - (Life / MaxLife), 0f, 1f) : 1f;

        /// <summary>
        /// Gets the current interpolated color between <see cref="Color"/> and <see cref="EndColor"/> based on lifetime progress.
        /// </summary>
        public readonly Color CurrentColor => Color.Lerp(Color, EndColor, NormalizedProgress);

        /// <summary>
        /// Gets the current interpolated size between <see cref="Size"/> and <see cref="EndSize"/> based on lifetime progress.
        /// </summary>
        public readonly float CurrentSize => MathHelper.Lerp(Size, EndSize, NormalizedProgress);
    }

    /// <summary>
    /// Represents an individual 3D expanding shockwave ring in world space.
    /// Memory layout is tightly packed to avoid GC allocations during lifecycle updates.
    /// </summary>
    public struct ShockwaveRing
    {
        /// <summary>
        /// 3D center position of the shockwave ring in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal orientation vector of the shockwave plane.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Initial ring radius at spawn time in world units.
        /// </summary>
        public float StartRadius;

        /// <summary>
        /// Final maximum ring radius at completion in world units.
        /// </summary>
        public float MaxRadius;

        /// <summary>
        /// Thickness of the ring edge in world units.
        /// </summary>
        public float Thickness;

        /// <summary>
        /// Color tint at spawn time.
        /// </summary>
        public Color StartColor;

        /// <summary>
        /// Color tint at death time.
        /// </summary>
        public Color EndColor;

        /// <summary>
        /// Remaining lifetime in seconds.
        /// </summary>
        public float Life;

        /// <summary>
        /// Initial total lifetime in seconds.
        /// </summary>
        public float MaxLife;

        /// <summary>
        /// Indicates whether this shockwave slot is currently active and rendered.
        /// </summary>
        public bool Active;

        /// <summary>
        /// Normalized lifetime progression [0.0 = spawn, 1.0 = death].
        /// </summary>
        public readonly float NormalizedProgress => MaxLife > 0.0001f ? MathHelper.Clamp(1f - (Life / MaxLife), 0f, 1f) : 1f;

        /// <summary>
        /// Current radius evaluated using smooth ease-out expansion: 1 - (1 - t)^2.
        /// </summary>
        public readonly float CurrentRadius
        {
            get
            {
                float t = NormalizedProgress;
                float easeOut = 1.0f - (1.0f - t) * (1.0f - t);
                return MathHelper.Lerp(StartRadius, MaxRadius, easeOut);
            }
        }

        /// <summary>
        /// Current interpolated color with smooth Hermite alpha decay.
        /// </summary>
        public readonly Color CurrentColor
        {
            get
            {
                float t = NormalizedProgress;
                float alphaDecay = 1.0f - (t * t * (3.0f - 2.0f * t));
                Color c = Color.Lerp(StartColor, EndColor, t);
                return c * alphaDecay;
            }
        }
    }

    /// <summary>
    /// High-performance 3D/2.5D Particle System with fixed-size pre-allocated particle & shockwave pools
    /// ensuring Zero GC memory allocation during frame Updates and Draws.
    /// Features calibrated world-unit billboard sizing, procedural texture generators (lasers, Gaussian flares, shockwaves),
    /// and dual SpriteBatch / 3D Quad rendering pipelines.
    /// </summary>
    public class ParticleSystem3D : IDisposable
    {
        /// <summary>
        /// Default particle pool capacity.
        /// </summary>
        public const int DefaultMaxParticles = 2048;

        /// <summary>
        /// Default shockwave ring pool capacity.
        /// </summary>
        public const int DefaultMaxShockwaves = 64;

        private readonly Particle3D[] _particles;
        private readonly int _maxParticles;
        private int _activeCount;
        private int _searchIndex;

        // Shockwave Ring Pool
        private readonly ShockwaveRing[] _shockwaveRings;
        private readonly int _maxShockwaveRings;
        private int _activeShockwaveCount;
        private int _shockwaveSearchIndex;

        private readonly Random _random;

        // Pre-allocated buffers for 3D DrawUserIndexedPrimitives (Zero-GC Draw3D)
        private readonly VertexPositionColorTexture[] _vertices;
        private readonly short[] _indices;
        private BasicEffect? _basicEffect;
        private Texture2D? _defaultTexture;
        private Texture2D? _shockwaveRingTexture;
        private bool _ownsDefaultTexture;
        private bool _ownsShockwaveTexture;
        private bool _disposed;

        #region Properties

        /// <summary>
        /// Gets the maximum capacity of the particle pool.
        /// </summary>
        public int MaxParticles => _maxParticles;

        /// <summary>
        /// Gets the number of currently active particles.
        /// </summary>
        public int ActiveParticlesCount => _activeCount;

        /// <summary>
        /// Gets the maximum capacity of the shockwave ring pool.
        /// </summary>
        public int MaxShockwaveRings => _maxShockwaveRings;

        /// <summary>
        /// Gets the number of currently active shockwave rings.
        /// </summary>
        public int ActiveShockwaveRingsCount => _activeShockwaveCount;

        /// <summary>
        /// Optional global gravity acceleration vector applied to all active particles during update. Default is Vector3.Zero.
        /// </summary>
        public Vector3 Gravity { get; set; } = Vector3.Zero;

        /// <summary>
        /// Linear velocity damping coefficient [0.0 = no drag, 1.0 = instant stop per second]. Default is 0.0f.
        /// </summary>
        public float Drag { get; set; } = 0.0f;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ParticleSystem3D"/> with the specified maximum particle and shockwave pool capacities.
        /// </summary>
        /// <param name="maxParticles">Maximum simultaneous particles (defaults to 2048).</param>
        /// <param name="maxShockwaves">Maximum simultaneous shockwave rings (defaults to 64).</param>
        public ParticleSystem3D(int maxParticles = DefaultMaxParticles, int maxShockwaves = DefaultMaxShockwaves)
        {
            _maxParticles = Math.Max(1, maxParticles);
            _particles = new Particle3D[_maxParticles];

            _maxShockwaveRings = Math.Max(1, maxShockwaves);
            _shockwaveRings = new ShockwaveRing[_maxShockwaveRings];

            // Allocate vertex buffer for particles plus shockwaves
            int totalQuads = _maxParticles + _maxShockwaveRings;
            _vertices = new VertexPositionColorTexture[totalQuads * 4];
            _indices = new short[totalQuads * 6];

            _random = new Random();
            _activeCount = 0;
            _searchIndex = 0;
            _activeShockwaveCount = 0;
            _shockwaveSearchIndex = 0;

            // Pre-calculate fixed quad indices for triangle list: (0, 1, 2) and (0, 2, 3)
            for (int i = 0; i < totalQuads; i++)
            {
                short baseVertex = (short)(i * 4);
                int baseIndex = i * 6;

                _indices[baseIndex + 0] = (short)(baseVertex + 0);
                _indices[baseIndex + 1] = (short)(baseVertex + 1);
                _indices[baseIndex + 2] = (short)(baseVertex + 2);

                _indices[baseIndex + 3] = (short)(baseVertex + 0);
                _indices[baseIndex + 4] = (short)(baseVertex + 2);
                _indices[baseIndex + 5] = (short)(baseVertex + 3);
            }
        }

        #endregion

        #region Emission Methods

        /// <summary>
        /// Emits a single particle into the system pool.
        /// </summary>
        /// <param name="particle">Particle configuration to spawn.</param>
        /// <returns>True if the particle was successfully spawned, or false if the pool is currently full.</returns>
        public bool Emit(in Particle3D particle)
        {
            for (int i = 0; i < _maxParticles; i++)
            {
                int index = (_searchIndex + i) % _maxParticles;
                if (!_particles[index].Active)
                {
                    _particles[index] = particle;
                    _particles[index].Active = true;
                    _activeCount++;
                    _searchIndex = (index + 1) % _maxParticles;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Emits a customized particle with explicit kinematic and aesthetic parameters in world units.
        /// </summary>
        public bool EmitCustom(
            Vector3 position,
            Vector3 velocity,
            Vector3 acceleration,
            Color startColor,
            Color endColor,
            float startSize,
            float endSize,
            float lifetime,
            float rotation = 0f,
            float rotationSpeed = 0f)
        {
            Particle3D p = new Particle3D
            {
                Position = position,
                Velocity = velocity,
                Acceleration = acceleration,
                Color = startColor,
                EndColor = endColor,
                Size = startSize,
                EndSize = endSize,
                Rotation = rotation,
                RotationSpeed = rotationSpeed,
                Life = Math.Max(0.001f, lifetime),
                MaxLife = Math.Max(0.001f, lifetime),
                Active = true
            };

            return Emit(in p);
        }

        /// <summary>
        /// Emits an expanding 3D shockwave ring into the dedicated shockwave pool with smooth ease-out expansion and alpha decay.
        /// </summary>
        /// <param name="position">3D center position in world space.</param>
        /// <param name="startRadius">Initial radius in world units (defaults to 0.5f).</param>
        /// <param name="maxRadius">Maximum expanded radius in world units (defaults to 15.0f).</param>
        /// <param name="lifetime">Duration of shockwave in seconds (defaults to 0.55f).</param>
        /// <param name="startColor">Initial glow color tint (defaults to electric cyan).</param>
        /// <param name="endColor">Final edge fade color tint (defaults to deep blue transparent).</param>
        /// <param name="thickness">Ring border thickness in world units (defaults to 0.6f).</param>
        /// <param name="normal">Orientation normal of the shockwave ring plane (defaults to UnitZ).</param>
        /// <returns>True if shockwave was emitted, false if pool is saturated.</returns>
        public bool EmitShockwaveRing(
            Vector3 position,
            float startRadius = 0.5f,
            float maxRadius = 15.0f,
            float lifetime = 0.55f,
            Color? startColor = null,
            Color? endColor = null,
            float thickness = 0.6f,
            Vector3? normal = null)
        {
            Color sColor = startColor ?? new Color(130, 225, 255, 255);
            Color eColor = endColor ?? new Color((int)(sColor.R * 0.2f), (int)(sColor.G * 0.5f), (int)sColor.B, 0);

            for (int i = 0; i < _maxShockwaveRings; i++)
            {
                int index = (_shockwaveSearchIndex + i) % _maxShockwaveRings;
                if (!_shockwaveRings[index].Active)
                {
                    _shockwaveRings[index] = new ShockwaveRing
                    {
                        Position = position,
                        Normal = normal ?? Vector3.UnitZ,
                        StartRadius = Math.Max(0.01f, startRadius),
                        MaxRadius = Math.Max(startRadius + 0.1f, maxRadius),
                        Thickness = thickness,
                        StartColor = sColor,
                        EndColor = eColor,
                        Life = Math.Max(0.001f, lifetime),
                        MaxLife = Math.Max(0.001f, lifetime),
                        Active = true
                    };
                    _activeShockwaveCount++;
                    _shockwaveSearchIndex = (index + 1) % _maxShockwaveRings;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Emits a multi-stage explosive burst with calibrated world-unit billboard sizes
        /// (bright core 3-6f, high-velocity debris 1.5-3.5f, and smoke embers 2.0-4.5f).
        /// </summary>
        /// <param name="position">World-space explosion origin point.</param>
        /// <param name="count">Total number of particles to spawn (defaults to 50).</param>
        /// <param name="startColor">Initial core flash color tint (defaults to bright yellow-white).</param>
        /// <param name="endColor">Fade-out debris color tint (defaults to fiery orange-red transparent).</param>
        /// <param name="speed">Initial burst radial velocity magnitude (defaults to 200f).</param>
        public void EmitExplosion(
            Vector3 position,
            int count = 50,
            Color? startColor = null,
            Color? endColor = null,
            float speed = 200f)
        {
            Color coreStart = startColor ?? new Color(255, 245, 200, 255);
            Color coreEnd = endColor ?? new Color(255, 80, 20, 0);

            int coreCount = Math.Max(1, (int)(count * 0.3f));
            int debrisCount = Math.Max(1, (int)(count * 0.45f));
            int smokeCount = Math.Max(0, count - coreCount - debrisCount);

            // 1. Fiery incandescent core particles (Calibrated: 3.0f - 6.0f world units)
            for (int i = 0; i < coreCount; i++)
            {
                Vector3 dir = GetRandomDirection3D();
                float spd = speed * (0.8f + (float)_random.NextDouble() * 0.6f);
                float life = 0.25f + (float)_random.NextDouble() * 0.35f;
                float startSize = 3.0f + (float)_random.NextDouble() * 3.0f; // 3 - 6f
                float endSize = startSize * (1.6f + (float)_random.NextDouble() * 0.4f);

                EmitCustom(
                    position: position,
                    velocity: dir * spd,
                    acceleration: -dir * (spd * 1.5f), // rapid deceleration
                    startColor: coreStart,
                    endColor: coreEnd,
                    startSize: startSize,
                    endSize: endSize,
                    lifetime: life,
                    rotation: (float)_random.NextDouble() * MathHelper.TwoPi,
                    rotationSpeed: ((float)_random.NextDouble() - 0.5f) * 6f
                );
            }

            // 2. High-speed projectile debris / sparks (Calibrated: 1.5f - 3.5f world units)
            Color sparkColor = new Color(255, 200, 80, 255);
            Color sparkEndColor = new Color(180, 40, 10, 0);
            for (int i = 0; i < debrisCount; i++)
            {
                Vector3 dir = GetRandomDirection3D();
                float spd = speed * (1.2f + (float)_random.NextDouble() * 1.0f);
                float life = 0.5f + (float)_random.NextDouble() * 0.7f;
                float size = 1.5f + (float)_random.NextDouble() * 2.0f; // 1.5 - 3.5f

                EmitCustom(
                    position: position,
                    velocity: dir * spd,
                    acceleration: new Vector3(0f, -80f, 0f), // gravity pulling debris down
                    startColor: sparkColor,
                    endColor: sparkEndColor,
                    startSize: size,
                    endSize: 0.4f,
                    lifetime: life,
                    rotation: (float)_random.NextDouble() * MathHelper.TwoPi,
                    rotationSpeed: ((float)_random.NextDouble() - 0.5f) * 12f
                );
            }

            // 3. Fading smoke clouds / rising embers (Calibrated: 2.0f - 4.5f world units)
            Color smokeStart = new Color(80, 80, 80, 180);
            Color smokeEnd = new Color(20, 20, 20, 0);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector3 dir = GetRandomDirection3D();
                float spd = speed * (0.15f + (float)_random.NextDouble() * 0.25f);
                float life = 0.8f + (float)_random.NextDouble() * 0.9f;
                float startSize = 2.0f + (float)_random.NextDouble() * 2.5f; // 2.0 - 4.5f
                float endSize = startSize * 2.2f;

                EmitCustom(
                    position: position + dir * 1.5f,
                    velocity: (dir * spd) + new Vector3(0f, 25f, 0f), // thermal updraft
                    acceleration: new Vector3(0f, 10f, 0f),
                    startColor: smokeStart,
                    endColor: smokeEnd,
                    startSize: startSize,
                    endSize: endSize,
                    lifetime: life,
                    rotation: (float)_random.NextDouble() * MathHelper.TwoPi,
                    rotationSpeed: ((float)_random.NextDouble() - 0.5f) * 2f
                );
            }
        }

        /// <summary>
        /// Emits a directional spray of fast sparks spreading in a cone around a target trajectory.
        /// Billboard sizes are calibrated to world units (0.8f - 2.2f).
        /// </summary>
        /// <param name="position">World-space spawn origin.</param>
        /// <param name="direction">Central spray direction vector.</param>
        /// <param name="spreadAngleDegrees">Cone half-angle spread in degrees (defaults to 30).</param>
        /// <param name="count">Number of sparks to generate (defaults to 25).</param>
        /// <param name="color">Spark color tint (defaults to incandescent yellow-cyan).</param>
        public void EmitSparks(
            Vector3 position,
            Vector3 direction,
            float spreadAngleDegrees = 30f,
            int count = 25,
            Color? color = null)
        {
            Vector3 normalizedDir = direction.LengthSquared() > 1e-6f ? Vector3.Normalize(direction) : Vector3.UnitY;
            Color sparkStart = color ?? new Color(255, 230, 120, 255);
            Color sparkEnd = new Color((int)sparkStart.R, (int)(sparkStart.G * 0.3f), 0, 0);
            float maxAngleRad = MathHelper.ToRadians(Math.Max(0.1f, spreadAngleDegrees));

            for (int i = 0; i < count; i++)
            {
                Vector3 sprayDir = GetRandomConeDirection(normalizedDir, maxAngleRad);
                float speed = 180f + (float)_random.NextDouble() * 260f;
                float life = 0.2f + (float)_random.NextDouble() * 0.45f;
                float startSize = 0.8f + (float)_random.NextDouble() * 1.4f; // 0.8 - 2.2f

                EmitCustom(
                    position: position,
                    velocity: sprayDir * speed,
                    acceleration: new Vector3(0f, -120f, 0f), // spark gravity drop
                    startColor: sparkStart,
                    endColor: sparkEnd,
                    startSize: startSize,
                    endSize: 0.15f,
                    lifetime: life,
                    rotation: (float)_random.NextDouble() * MathHelper.TwoPi,
                    rotationSpeed: ((float)_random.NextDouble() - 0.5f) * 15f
                );
            }
        }

        /// <summary>
        /// Emits a circular shockwave composed of 48 calibrated world-unit particles.
        /// </summary>
        /// <param name="position">Center of the shockwave ring.</param>
        /// <param name="radius">Initial ring spawn radius (defaults to 50f).</param>
        /// <param name="color">Shockwave energy tint (defaults to cyan electric glow).</param>
        /// <param name="expansionSpeed">Radial outward expansion speed (defaults to 300f).</param>
        public void EmitShockwave(
            Vector3 position,
            float radius = 50f,
            Color? color = null,
            float expansionSpeed = 300f)
        {
            Color startColor = color ?? new Color(120, 220, 255, 240);
            Color endColor = new Color((int)startColor.R, (int)startColor.G, (int)startColor.B, 0);
            int segments = 48;
            float step = MathHelper.TwoPi / segments;
            float life = 0.35f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                Vector3 ringDir = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f);
                Vector3 spawnPos = position + (ringDir * (radius * 0.15f));

                EmitCustom(
                    position: spawnPos,
                    velocity: ringDir * expansionSpeed,
                    acceleration: -ringDir * (expansionSpeed * 0.6f), // gradual decay
                    startColor: startColor,
                    endColor: endColor,
                    startSize: 2.5f,
                    endSize: 0.4f,
                    lifetime: life,
                    rotation: angle,
                    rotationSpeed: 0f
                );
            }
        }

        /// <summary>
        /// Emits an engine thruster trail plume trailing behind a moving spacecraft or projectile.
        /// Calibrated to world units (1.5f - 3.0f).
        /// </summary>
        /// <param name="position">World position of engine exhaust nozzle.</param>
        /// <param name="backwardDir">Direction pointing backward from the vessel.</param>
        /// <param name="speed">Exhaust gas speed (defaults to 100f).</param>
        /// <param name="flameColor">Core plasma tint (defaults to electric cyan-blue).</param>
        public void EmitThrusterTrail(
            Vector3 position,
            Vector3 backwardDir,
            float speed = 100f,
            Color? flameColor = null)
        {
            Vector3 dir = backwardDir.LengthSquared() > 1e-6f ? Vector3.Normalize(backwardDir) : -Vector3.UnitY;
            Color coreCol = flameColor ?? new Color(90, 190, 255, 255);
            Color endCol = new Color((int)(coreCol.R * 0.3f), (int)(coreCol.G * 0.5f), (int)coreCol.B, 0);

            // Jitter velocity direction slightly within 8 degrees
            Vector3 sprayDir = GetRandomConeDirection(dir, MathHelper.ToRadians(8f));
            float spd = speed * (0.85f + (float)_random.NextDouble() * 0.3f);
            float life = 0.15f + (float)_random.NextDouble() * 0.15f;
            float startSize = 1.5f + (float)_random.NextDouble() * 1.5f; // 1.5 - 3.0f

            EmitCustom(
                position: position + (GetRandomDirection3D() * 0.5f),
                velocity: sprayDir * spd,
                acceleration: -sprayDir * (spd * 0.5f),
                startColor: coreCol,
                endColor: endCol,
                startSize: startSize,
                endSize: 0.3f,
                lifetime: life,
                rotation: (float)_random.NextDouble() * MathHelper.TwoPi,
                rotationSpeed: ((float)_random.NextDouble() - 0.5f) * 8f
            );
        }

        #endregion

        #region Kinematics & Update Loop

        /// <summary>
        /// Updates the kinematics, rotations, lifetimes, and color/size interpolations of all active particles and shockwave rings.
        /// Runs completely allocation-free with zero garbage collection overhead.
        /// </summary>
        /// <param name="gameTime">The current frame timing snapshot.</param>
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (dt <= 0f) return;

            // 1. Update Particles
            if (_activeCount > 0)
            {
                Vector3 gravityStep = Gravity * dt;
                float dragFactor = Math.Max(0f, 1f - (Drag * dt));

                for (int i = 0; i < _maxParticles; i++)
                {
                    if (!_particles[i].Active) continue;

                    _particles[i].Life -= dt;
                    if (_particles[i].Life <= 0f)
                    {
                        _particles[i].Active = false;
                        _activeCount--;
                        continue;
                    }

                    // Kinematic integration: Accel -> Velocity -> Position
                    _particles[i].Velocity += (_particles[i].Acceleration * dt) + gravityStep;
                    if (Drag > 0f)
                    {
                        _particles[i].Velocity *= dragFactor;
                    }

                    _particles[i].Position += _particles[i].Velocity * dt;
                    _particles[i].Rotation += _particles[i].RotationSpeed * dt;
                }
            }

            // 2. Update Shockwave Rings
            if (_activeShockwaveCount > 0)
            {
                for (int i = 0; i < _maxShockwaveRings; i++)
                {
                    if (!_shockwaveRings[i].Active) continue;

                    _shockwaveRings[i].Life -= dt;
                    if (_shockwaveRings[i].Life <= 0f)
                    {
                        _shockwaveRings[i].Active = false;
                        _activeShockwaveCount--;
                    }
                }
            }
        }

        #endregion

        #region Rendering Pipelines

        /// <summary>
        /// Projects 3D particles and shockwave rings onto the 2D screen viewport and renders them using <see cref="SpriteBatch"/>.
        /// Ideal for 2.5D gameplay layers and lightweight billboard particles.
        /// </summary>
        /// <param name="spriteBatch">Active SpriteBatch instance.</param>
        /// <param name="camera">Camera3D providing view and projection transformations.</param>
        /// <param name="particleTexture">Optional custom particle sprite texture (defaults to smooth glowing radial circle).</param>
        /// <param name="blendState">Blend state (defaults to <see cref="BlendState.Additive"/> for glowing visual energy).</param>
        public void Draw(
            SpriteBatch spriteBatch,
            Camera3D camera,
            Texture2D? particleTexture = null,
            BlendState? blendState = null)
        {
            if (_activeCount == 0 && _activeShockwaveCount == 0) return;

            Texture2D tex = particleTexture ?? GetOrCreateDefaultTexture(spriteBatch.GraphicsDevice);
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
            BlendState activeBlend = blendState ?? BlendState.Additive;
            Viewport vp = spriteBatch.GraphicsDevice.Viewport;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                activeBlend,
                SamplerState.LinearClamp,
                DepthStencilState.DepthRead,
                RasterizerState.CullNone
            );

            float fovHalfTan = MathF.Tan(camera.FieldOfView * 0.5f);
            float viewportHeight = vp.Height;

            // Render active particles
            if (_activeCount > 0)
            {
                for (int i = 0; i < _maxParticles; i++)
                {
                    if (!_particles[i].Active) continue;

                    Vector3 screen3D = camera.WorldToScreen(_particles[i].Position, vp);

                    // Discard particles behind camera near plane or beyond far plane
                    if (screen3D.Z < 0f || screen3D.Z > 1f) continue;

                    // Perspective scale calculation
                    float distance = Vector3.Distance(camera.EffectivePosition, _particles[i].Position);
                    if (distance < 0.01f) distance = 0.01f;

                    float projectedPixelHeight = (_particles[i].CurrentSize / distance) * (viewportHeight / (2f * fovHalfTan));
                    float scale = projectedPixelHeight / tex.Height;

                    if (scale <= 0.001f) continue;

                    spriteBatch.Draw(
                        tex,
                        new Vector2(screen3D.X, screen3D.Y),
                        null,
                        _particles[i].CurrentColor,
                        _particles[i].Rotation,
                        origin,
                        scale,
                        SpriteEffects.None,
                        screen3D.Z
                    );
                }
            }

            // Render active shockwave rings
            if (_activeShockwaveCount > 0)
            {
                Texture2D ringTex = GetOrCreateShockwaveRingTexture(spriteBatch.GraphicsDevice);
                Vector2 ringOrigin = new Vector2(ringTex.Width * 0.5f, ringTex.Height * 0.5f);

                for (int i = 0; i < _maxShockwaveRings; i++)
                {
                    if (!_shockwaveRings[i].Active) continue;

                    Vector3 screen3D = camera.WorldToScreen(_shockwaveRings[i].Position, vp);
                    if (screen3D.Z < 0f || screen3D.Z > 1f) continue;

                    float distance = Vector3.Distance(camera.EffectivePosition, _shockwaveRings[i].Position);
                    if (distance < 0.01f) distance = 0.01f;

                    float diameter = _shockwaveRings[i].CurrentRadius * 2.0f;
                    float projectedPixelHeight = (diameter / distance) * (viewportHeight / (2f * fovHalfTan));
                    float scale = projectedPixelHeight / ringTex.Height;

                    if (scale <= 0.001f) continue;

                    spriteBatch.Draw(
                        ringTex,
                        new Vector2(screen3D.X, screen3D.Y),
                        null,
                        _shockwaveRings[i].CurrentColor,
                        0f,
                        ringOrigin,
                        scale,
                        SpriteEffects.None,
                        screen3D.Z
                    );
                }
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Renders active particles and shockwave rings as camera-aligned 3D billboard quads directly on the GPU using
        /// pre-allocated vertex/index buffers and <see cref="BasicEffect"/>.
        /// Offers maximum 3D depth-sorting accuracy and zero GC heap allocations.
        /// </summary>
        /// <param name="graphicsDevice">Target GraphicsDevice.</param>
        /// <param name="camera">Camera3D providing view and projection transformations.</param>
        /// <param name="particleTexture">Optional custom particle sprite texture (defaults to smooth glowing radial circle).</param>
        /// <param name="blendState">Blend state (defaults to <see cref="BlendState.Additive"/> for intense energy and sparks).</param>
        public void Draw3D(
            GraphicsDevice graphicsDevice,
            Camera3D camera,
            Texture2D? particleTexture = null,
            BlendState? blendState = null)
        {
            if (_activeCount == 0 && _activeShockwaveCount == 0) return;

            Texture2D tex = particleTexture ?? GetOrCreateDefaultTexture(graphicsDevice);

            // Compute camera billboard alignment vectors in world space
            Matrix invView = Matrix.Invert(camera.ViewMatrix);
            Vector3 camRight = Vector3.Normalize(invView.Right);
            Vector3 camUp = Vector3.Normalize(invView.Up);

            int quadIndex = 0;

            // 1. Pack Particle Quads
            if (_activeCount > 0)
            {
                for (int i = 0; i < _maxParticles; i++)
                {
                    if (!_particles[i].Active) continue;

                    float halfSize = _particles[i].CurrentSize * 0.5f;
                    Color col = _particles[i].CurrentColor;
                    Vector3 pos = _particles[i].Position;

                    Vector3 r;
                    Vector3 u;

                    if (Math.Abs(_particles[i].Rotation) > 1e-4f)
                    {
                        float cos = MathF.Cos(_particles[i].Rotation);
                        float sin = MathF.Sin(_particles[i].Rotation);
                        r = (camRight * cos + camUp * sin) * halfSize;
                        u = (-camRight * sin + camUp * cos) * halfSize;
                    }
                    else
                    {
                        r = camRight * halfSize;
                        u = camUp * halfSize;
                    }

                    int vBase = quadIndex * 4;

                    // Vertex 0: Top-Left (UV 0, 0)
                    _vertices[vBase + 0].Position = pos - r + u;
                    _vertices[vBase + 0].Color = col;
                    _vertices[vBase + 0].TextureCoordinate = new Vector2(0f, 0f);

                    // Vertex 1: Top-Right (UV 1, 0)
                    _vertices[vBase + 1].Position = pos + r + u;
                    _vertices[vBase + 1].Color = col;
                    _vertices[vBase + 1].TextureCoordinate = new Vector2(1f, 0f);

                    // Vertex 2: Bottom-Right (UV 1, 1)
                    _vertices[vBase + 2].Position = pos + r - u;
                    _vertices[vBase + 2].Color = col;
                    _vertices[vBase + 2].TextureCoordinate = new Vector2(1f, 1f);

                    // Vertex 3: Bottom-Left (UV 0, 1)
                    _vertices[vBase + 3].Position = pos - r - u;
                    _vertices[vBase + 3].Color = col;
                    _vertices[vBase + 3].TextureCoordinate = new Vector2(0f, 1f);

                    quadIndex++;
                }
            }

            // Draw particle quads batch
            if (quadIndex > 0)
            {
                RenderQuads(graphicsDevice, camera, tex, blendState, 0, quadIndex);
            }

            // 2. Pack Shockwave Ring Quads
            if (_activeShockwaveCount > 0)
            {
                Texture2D ringTex = GetOrCreateShockwaveRingTexture(graphicsDevice);
                int ringQuadStart = 0;
                int ringQuadCount = 0;

                for (int i = 0; i < _maxShockwaveRings; i++)
                {
                    if (!_shockwaveRings[i].Active) continue;

                    float radius = _shockwaveRings[i].CurrentRadius;
                    Color col = _shockwaveRings[i].CurrentColor;
                    Vector3 pos = _shockwaveRings[i].Position;

                    Vector3 r = camRight * radius;
                    Vector3 u = camUp * radius;

                    int vBase = ringQuadCount * 4;

                    _vertices[vBase + 0].Position = pos - r + u;
                    _vertices[vBase + 0].Color = col;
                    _vertices[vBase + 0].TextureCoordinate = new Vector2(0f, 0f);

                    _vertices[vBase + 1].Position = pos + r + u;
                    _vertices[vBase + 1].Color = col;
                    _vertices[vBase + 1].TextureCoordinate = new Vector2(1f, 0f);

                    _vertices[vBase + 2].Position = pos + r - u;
                    _vertices[vBase + 2].Color = col;
                    _vertices[vBase + 2].TextureCoordinate = new Vector2(1f, 1f);

                    _vertices[vBase + 3].Position = pos - r - u;
                    _vertices[vBase + 3].Color = col;
                    _vertices[vBase + 3].TextureCoordinate = new Vector2(0f, 1f);

                    ringQuadCount++;
                }

                if (ringQuadCount > 0)
                {
                    RenderQuads(graphicsDevice, camera, ringTex, blendState, ringQuadStart, ringQuadCount);
                }
            }
        }

        private void RenderQuads(GraphicsDevice graphicsDevice, Camera3D camera, Texture2D texture, BlendState? blendState, int startQuad, int quadCount)
        {
            _basicEffect ??= new BasicEffect(graphicsDevice);
            _basicEffect.World = Matrix.Identity;
            _basicEffect.View = camera.ViewMatrix;
            _basicEffect.Projection = camera.ProjectionMatrix;
            _basicEffect.TextureEnabled = true;
            _basicEffect.Texture = texture;
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.LightingEnabled = false;

            graphicsDevice.BlendState = blendState ?? BlendState.Additive;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertices,
                    startQuad * 4,
                    quadCount * 4,
                    _indices,
                    startQuad * 6,
                    quadCount * 2
                );
            }
        }

        #endregion

        #region Procedural Texture Generators

        /// <summary>
        /// Procedurally generates an elongated teardrop/capsule laser beam texture with an intense white/bright core,
        /// smooth radial and longitudinal edge falloff, and vibrant outer colored glow.
        /// </summary>
        /// <param name="device">Active MonoGame GraphicsDevice.</param>
        /// <param name="width">Laser texture width in pixels (default: 64).</param>
        /// <param name="height">Laser texture height in pixels (default: 16).</param>
        /// <param name="coreColor">Inner core color (defaults to pure bright white).</param>
        /// <param name="glowColor">Outer energy glow color (defaults to electric cyan).</param>
        /// <returns>A new Texture2D containing the capsule laser sprite.</returns>
        public static Texture2D CreateLaserTexture(
            GraphicsDevice device,
            int width = 64,
            int height = 16,
            Color? coreColor = null,
            Color? glowColor = null)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (width <= 0) width = 64;
            if (height <= 0) height = 16;

            Color cCore = coreColor ?? new Color(255, 255, 255, 255);
            Color cGlow = glowColor ?? new Color(90, 210, 255, 255);

            Texture2D texture = new Texture2D(device, width, height, false, SurfaceFormat.Color);
            Color[] data = new Color[width * height];

            float cy = (height - 1) * 0.5f;
            float radius = cy;
            float capLeft = radius;
            float capRight = (width - 1) - radius;

            for (int y = 0; y < height; y++)
            {
                float dy = (y - cy);
                int rowOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    // Find closest point on horizontal capsule centerline
                    float clampedX = Math.Clamp((float)x, capLeft, capRight);
                    float dx = x - clampedX;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float normDist = dist / radius;

                    if (normDist <= 1.0f)
                    {
                        // Outer glow falloff (smooth cosine profile)
                        float glowFactor = MathF.Cos(normDist * MathHelper.PiOver2);
                        glowFactor = glowFactor * glowFactor;

                        // Inner core falloff (intense bright white center)
                        float coreDist = dist / (radius * 0.45f);
                        float coreFactor = Math.Clamp(1.0f - coreDist, 0f, 1f);
                        coreFactor = coreFactor * coreFactor;

                        // Blend between outer glow and inner hot core
                        Color baseColor = Color.Lerp(cGlow, cCore, coreFactor);
                        byte alpha = (byte)Math.Clamp((int)(glowFactor * 255f + 0.5f), 0, 255);
                        data[rowOffset + x] = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
                    }
                    else
                    {
                        data[rowOffset + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Procedurally generates a smooth Gaussian flare texture with an intense core,
        /// smooth exponential Gaussian distribution (sigma), and zero edge artifacting.
        /// </summary>
        /// <param name="device">Active MonoGame GraphicsDevice.</param>
        /// <param name="size">Square texture dimension in pixels (default: 64).</param>
        /// <param name="sigma">Gaussian spread standard deviation factor (default: 0.35f).</param>
        /// <param name="flareColor">Color tint of the flare (defaults to bright white).</param>
        /// <returns>A new Texture2D containing the Gaussian flare.</returns>
        public static Texture2D CreateGaussianFlareTexture(
            GraphicsDevice device,
            int size = 64,
            float sigma = 0.35f,
            Color? flareColor = null)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (size <= 0) size = 64;
            if (sigma <= 0.001f) sigma = 0.35f;

            Color baseCol = flareColor ?? Color.White;
            Texture2D texture = new Texture2D(device, size, size, false, SurfaceFormat.Color);
            Color[] data = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float invCenter = center > 0.001f ? 1.0f / center : 1.0f;
            float twoSigmaSq = 2.0f * sigma * sigma;

            for (int y = 0; y < size; y++)
            {
                float dy = (y - center) * invCenter;
                float dy2 = dy * dy;
                int rowOffset = y * size;

                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) * invCenter;
                    float rSq = dx * dx + dy2;
                    float r = MathF.Sqrt(rSq);

                    if (r <= 1.0f)
                    {
                        // Gaussian distribution
                        float g = MathF.Exp(-rSq / twoSigmaSq);

                        // Edge cutoff feather so alpha reaches zero at r = 1.0
                        float edgeFeather = MathF.Cos(r * MathHelper.PiOver2);
                        float finalIntensity = g * edgeFeather;

                        byte alpha = (byte)Math.Clamp((int)(finalIntensity * 255f + 0.5f), 0, 255);
                        data[rowOffset + x] = new Color(baseCol.R, baseCol.G, baseCol.B, alpha);
                    }
                    else
                    {
                        data[rowOffset + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Procedurally generates an anti-aliased hollow circular shockwave ring texture.
        /// </summary>
        /// <param name="device">Active MonoGame GraphicsDevice.</param>
        /// <param name="size">Square texture dimension in pixels (default: 128).</param>
        /// <param name="ringWidth">Normalized ring border thickness [0.0 - 1.0] (default: 0.18f).</param>
        /// <param name="ringColor">Color tint of the shockwave ring (defaults to white).</param>
        /// <returns>A new Texture2D containing the shockwave ring.</returns>
        public static Texture2D CreateShockwaveRingTexture(
            GraphicsDevice device,
            int size = 128,
            float ringWidth = 0.18f,
            Color? ringColor = null)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (size <= 0) size = 128;
            ringWidth = MathHelper.Clamp(ringWidth, 0.02f, 0.5f);

            Color baseCol = ringColor ?? Color.White;
            Texture2D texture = new Texture2D(device, size, size, false, SurfaceFormat.Color);
            Color[] data = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float maxR = center;
            float innerR = maxR * (1.0f - ringWidth);
            float midR = (maxR + innerR) * 0.5f;
            float halfWidth = (maxR - innerR) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                float dy = y - center;
                int rowOffset = y * size;

                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist >= innerR && dist <= maxR)
                    {
                        // Peak intensity at midR, smooth cosine falloff to innerR and maxR
                        float dMid = Math.Abs(dist - midR) / halfWidth;
                        float falloff = MathF.Cos(dMid * MathHelper.PiOver2);
                        falloff = falloff * falloff;

                        byte alpha = (byte)Math.Clamp((int)(falloff * 255f + 0.5f), 0, 255);
                        data[rowOffset + x] = new Color(baseCol.R, baseCol.G, baseCol.B, alpha);
                    }
                    else
                    {
                        data[rowOffset + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        // Convenience aliases
        public static Texture2D GenerateLaserTexture(GraphicsDevice device, int width = 64, int height = 16, Color? coreColor = null, Color? glowColor = null)
            => CreateLaserTexture(device, width, height, coreColor, glowColor);

        public static Texture2D GenerateGaussianFlareTexture(GraphicsDevice device, int size = 64, float sigma = 0.35f, Color? flareColor = null)
            => CreateGaussianFlareTexture(device, size, sigma, flareColor);

        public static Texture2D GenerateShockwaveRingTexture(GraphicsDevice device, int size = 128, float ringWidth = 0.18f, Color? ringColor = null)
            => CreateShockwaveRingTexture(device, size, ringWidth, ringColor);

        #endregion

        #region Helpers & Resource Management

        /// <summary>
        /// Clears all currently active particles and shockwave rings and resets the system pools.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _maxParticles; i++)
            {
                _particles[i].Active = false;
            }
            _activeCount = 0;
            _searchIndex = 0;

            for (int i = 0; i < _maxShockwaveRings; i++)
            {
                _shockwaveRings[i].Active = false;
            }
            _activeShockwaveCount = 0;
            _shockwaveSearchIndex = 0;
        }

        private Texture2D GetOrCreateDefaultTexture(GraphicsDevice device)
        {
            if (_defaultTexture != null && !_defaultTexture.IsDisposed && _defaultTexture.GraphicsDevice == device)
            {
                return _defaultTexture;
            }

            int size = 32;
            _defaultTexture = new Texture2D(device, size, size);
            Color[] data = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float maxRadius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxRadius;
                    float dy = (y - center) / maxRadius;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist <= 1.0f)
                    {
                        float falloff = MathF.Cos(dist * MathHelper.PiOver2);
                        falloff = falloff * falloff; // Smooth quadratic glowing edge
                        byte alpha = (byte)(falloff * 255f);
                        data[y * size + x] = new Color(255, 255, 255, (int)alpha);
                    }
                    else
                    {
                        data[y * size + x] = Color.Transparent;
                    }
                }
            }

            _defaultTexture.SetData(data);
            _ownsDefaultTexture = true;
            return _defaultTexture;
        }

        private Texture2D GetOrCreateShockwaveRingTexture(GraphicsDevice device)
        {
            if (_shockwaveRingTexture != null && !_shockwaveRingTexture.IsDisposed && _shockwaveRingTexture.GraphicsDevice == device)
            {
                return _shockwaveRingTexture;
            }

            _shockwaveRingTexture = CreateShockwaveRingTexture(device, 128, 0.18f);
            _ownsShockwaveTexture = true;
            return _shockwaveRingTexture;
        }

        private Vector3 GetRandomDirection3D()
        {
            float theta = (float)_random.NextDouble() * MathHelper.TwoPi;
            float phi = MathF.Acos(2f * (float)_random.NextDouble() - 1f);

            float sinPhi = MathF.Sin(phi);
            return new Vector3(
                sinPhi * MathF.Cos(theta),
                sinPhi * MathF.Sin(theta),
                MathF.Cos(phi)
            );
        }

        private Vector3 GetRandomConeDirection(Vector3 forward, float maxAngleRad)
        {
            float theta = (float)_random.NextDouble() * MathHelper.TwoPi;
            float angle = (float)_random.NextDouble() * maxAngleRad;

            // Find an orthogonal vector to forward
            Vector3 arbitrary = Math.Abs(forward.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, arbitrary));
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

            float sinAngle = MathF.Sin(angle);
            Vector3 offset = (right * MathF.Cos(theta) + up * MathF.Sin(theta)) * sinAngle;
            Vector3 result = (forward * MathF.Cos(angle)) + offset;
            return Vector3.Normalize(result);
        }

        /// <summary>
        /// Releases all managed and unmanaged GPU resources used by the particle system.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _basicEffect?.Dispose();
                _basicEffect = null;

                if (_ownsDefaultTexture && _defaultTexture != null && !_defaultTexture.IsDisposed)
                {
                    _defaultTexture.Dispose();
                    _defaultTexture = null;
                }

                if (_ownsShockwaveTexture && _shockwaveRingTexture != null && !_shockwaveRingTexture.IsDisposed)
                {
                    _shockwaveRingTexture.Dispose();
                    _shockwaveRingTexture = null;
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
