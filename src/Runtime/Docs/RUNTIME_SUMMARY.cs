using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoGameMaker.Runtime
{
    /// <summary>
    /// Legacy base interface for scripts. Kept for backwards compatibility.
    /// </summary>
    public interface IEntityScript : IDisposable
    {
        /// <summary>
        /// Initializes the script with entity context and properties.
        /// </summary>
        void Initialize(GameEntity entity, Dictionary<string, string> properties);

        /// <summary>
        /// Called once per frame to update behavior logic.
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Called once per frame to draw custom rendering overlays.
        /// </summary>
        void Draw(SpriteBatch spriteBatch);

        /// <summary>
        /// Called once per frame to draw UI overlays.
        /// </summary>
        void DrawUI(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// Base class for all runtime script behaviors. Inherit from this class to add custom entity logic.
    /// </summary>
    /// <example>
    /// <code>
    /// public class PlayerController : EntityBehavior
    /// {
    ///     public override void Awake()
    ///     {
    ///         // Initialization logic
    ///     }
    ///     public override void Update(GameTime gameTime)
    ///     {
    ///         // Update logic
    ///     }
    ///     public override void Dispose()
    ///     {
    ///         // Unsubscribe global events
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class EntityBehavior : IDisposable
    {
        public GameEntity Entity { get; } = null!;

        /// <summary>
        /// Key-value properties initialized from the Prefab configuration.
        /// </summary>
        public Dictionary<string, string> Properties { get; } = null!;

        /// <summary>
        /// Called when the entity behavior is instantiated and initialized.
        /// </summary>
        public virtual void Awake() { }

        /// <summary>
        /// Called once per frame to update behavior logic.
        /// </summary>
        public virtual void Update(GameTime gameTime) { }

        /// <summary>
        /// Called once per frame to draw custom rendering overlays.
        /// </summary>
        public virtual void Draw(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Called once per frame to draw UI overlays. Can be overridden to do standard SpriteBatch UI drawing.
        /// By default, calls the parameterless DrawUI() overload to support ImGui-based user interface drawing.
        /// </summary>
        public virtual void DrawUI(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Called once per frame to draw screen-space UI elements. Override this method to perform direct ImGui calls (e.g. ImGui.Begin, ImGui.Button).
        /// </summary>
        public virtual void DrawUI() { }

        /// <summary>
        /// Called dynamically when this entity collides with another entity.
        /// </summary>
        public virtual void OnCollision(GameEntity other) { }

        /// <summary>
        /// Disposes of resources (such as event subscriptions) held by this behavior.
        /// </summary>
        public virtual void Dispose() { }
    }

    /// <summary>
    /// Represents a live entity in the game world.
    /// </summary>
    public class GameEntity
    {
        /// <summary>
        /// The unique identifier of this entity.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Initializes a new instance of the GameEntity class with a newly generated Guid.
        /// </summary>
        public GameEntity() { }

        /// <summary>
        /// Initializes a new instance of the GameEntity class with a specific pre-existing Guid.
        /// </summary>
        /// <param name="id">The pre-existing Guid to assign to this entity.</param>
        public GameEntity(Guid id) { }

        /// <summary>
        /// Injects or updates the entity's Guid.
        /// </summary>
        /// <param name="id">The Guid to hydrate the entity with.</param>
        public void LoadState(Guid id) { }

        public string PrefabName { get; set; } = string.Empty;

        /// <summary>
        /// The active 2D Sprite Texture of this entity.
        /// </summary>
        public Texture2D Texture { get; set; } = null!;

        /// <summary>
        /// The current position coordinate of this entity in World Space.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The active behavior script attached to this entity, if any.
        /// </summary>
        public EntityBehavior Script { get; set; } = null!;

        /// <summary>
        /// Tag classification category used for query lookups and collision filtering.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// True if the entity is queued for deletion.
        /// </summary>
        public bool IsDestroyed { get; set; }

        /// <summary>
        /// The active sprite clip viewport sheet rectangle (null if full texture is drawn).
        /// </summary>
        public Rectangle? SourceRect { get; set; }

        /// <summary>
        /// Custom offset coordinates for the collision hitbox relative to the position coordinate.
        /// </summary>
        public Vector2 HitboxOffset { get; set; }

        /// <summary>
        /// Custom dimensions for the collision hitbox.
        /// </summary>
        public Vector2 HitboxSize { get; set; }

        /// <summary>
        /// Computes the final World Space axis-aligned bounding box (AABB) of the entity's collision mask.
        /// </summary>
        public Rectangle Bounds { get; }

        /// <summary>
        /// Starts playing a uniform grid-based spritesheet animation clip.
        /// </summary>
        /// <param name="frameWidth">The width of a single animation frame.</param>
        /// <param name="frameHeight">The height of a single animation frame.</param>
        /// <param name="startFrame">The 0-based index of the first frame.</param>
        /// <param name="endFrame">The 0-based index of the last frame.</param>
        /// <param name="fps">Frames per second.</param>
        public void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps) { }

        /// <summary>
        /// Updates the active animation frame based on the elapsed time.
        /// </summary>
        public void UpdateAnimation(GameTime gameTime) { }
    }

    /// <summary>
    /// Manages spawning, destruction, and dynamic queries of all active game entities.
    /// </summary>
    public static class EntityManager
    {
        /// <summary>
        /// List of all active entities currently loaded in the active scene.
        /// </summary>
        public static List<GameEntity> Entities { get; } = null!;

        /// <summary>
        /// Spawns a new entity instance from a prefab configuration.
        /// </summary>
        /// <example>
        /// <code>
        /// var player = EntityManager.Spawn("Player", new Vector2(100, 100));
        /// </code>
        /// </example>
        public static GameEntity Spawn(string prefabName, Vector2 position) => null!;

        /// <summary>
        /// Instantiates a new bare GameEntity with a freshly allocated Guid.
        /// </summary>
        public static GameEntity CreateEntity() => null!;

        /// <summary>
        /// Instantiates a bare GameEntity by forcing the injection of the provided Guid.
        /// </summary>
        public static GameEntity RestoreEntity(Guid id) => null!;

        /// <summary>
        /// Queues the specified entity for immediate destruction at the end of the frame.
        /// </summary>
        public static void Destroy(GameEntity entity) { }

        /// <summary>
        /// Queries the world to find the first entity carrying a specific tag that overlaps the caller's bounding box.
        /// </summary>
        public static GameEntity GetFirstColliding(GameEntity caller, string targetTag) => null!;

        /// <summary>
        /// Deterministically disposes and purges all script behaviors, clearing entity lists and static caches to prevent memory leaks during hot reload.
        /// </summary>
        public static void PurgeAllScripts() { }
    }

    /// <summary>
    /// Persistent global game state registry. Survs level transitions.
    /// </summary>
    public static class GameState
    {
        /// <summary>
        /// Raw data dictionary storage.
        /// </summary>
        public static Dictionary<string, object> Data { get; } = null!;

        /// <summary>
        /// Sets a persistent global value.
        /// </summary>
        public static void Set(string key, object value) { }

        /// <summary>
        /// Retrieves a persistent global value, with a type-fallback.
        /// </summary>
        public static T Get<T>(string key) => default!;

        /// <summary>
        /// Check if the registry has a value stored under the specified key.
        /// </summary>
        public static bool Has(string key) => false;

        /// <summary>
        /// Clear all persistent global data.
        /// </summary>
        public static void Clear() { }

        /// <summary>
        /// Saves the GameState dictionary data to a JSON file (by default "save_state.json") in the game root folder.
        /// Call this when the player saves the game or transitions levels to persist progress.
        /// </summary>
        public static void SaveToFile(string filename = "save_state.json") { }

        /// <summary>
        /// Loads the GameState dictionary data from a JSON file (by default "save_state.json").
        /// Call this at game launch or load checkpoints to restore player progress.
        /// </summary>
        public static void LoadFromFile(string filename = "save_state.json") { }
    }

    /// <summary>
    /// Manages loading and layout initialization of scenes.
    /// </summary>
    public static class SceneManager
    {
        /// <summary>
        /// The filename of the currently loaded scene (without path prefix or extension).
        /// </summary>
        public static string CurrentScene { get; } = string.Empty;

        /// <summary>
        /// Loads a new scene layout configuration file asynchronously.
        /// </summary>
        /// <example>
        /// <code>
        /// SceneManager.LoadScene("level_2");
        /// </code>
        /// </example>
        public static void LoadScene(string sceneName) { }
    }

    /// <summary>
    /// Handles 2D Camera viewport translation matrix calculations and boundary clamping.
    /// </summary>
    public static class Camera2D
    {
        /// <summary>
        /// The translation coordinate of the camera lens center in world space.
        /// </summary>
        public static Vector2 Position { get; set; }

        /// <summary>
        /// The camera scale zoom factor (defaults to 1.0f).
        /// </summary>
        public static float Zoom { get; set; }

        /// <summary>
        /// The camera lens rotation angle.
        /// </summary>
        public static float Rotation { get; set; }

        /// <summary>
        /// Gets the final calculated 2D view transformation matrix to pass to SpriteBatch.Begin().
        /// </summary>
        public static Matrix Transform { get; }

        /// <summary>
        /// Focuses the camera lens smoothly towards a world space target position.
        /// </summary>
        public static void LookAt(Vector2 targetPosition, int viewportWidth, int viewportHeight) { }
    }

    /// <summary>
    /// Perspective 3D and 2.5D Camera featuring trauma-based screen shake, 
    /// unprojection raycasting (mouse aiming), and target tracking.
    /// </summary>
    /// <example>
    /// <code>
    /// // Initialize camera
    /// var camera = new Camera3D(16f / 9f) { Position = new Vector3(0, -60, 120), Target = Vector3.Zero };
    /// 
    /// // Screen shake on explosion
    /// camera.AddTrauma(0.7f);
    /// 
    /// // Unproject mouse aim onto Z = 0 gameplay plane
    /// Vector3 aimWorld = camera.ScreenToWorldPlane(new Vector2(mouse.X, mouse.Y), planeZ: 0f);
    /// </code>
    /// </example>
    public class Camera3D
    {
        /// <summary>Base camera position in 3D world space.</summary>
        public Vector3 Position { get; set; } = new Vector3(0f, 0f, 100f);

        /// <summary>Base camera target in 3D world space.</summary>
        public Vector3 Target { get; set; } = Vector3.Zero;

        /// <summary>Camera up vector (defaults to Vector3.Up).</summary>
        public Vector3 Up { get; set; } = Vector3.Up;

        /// <summary>Vertical field of view in radians (default: 55 degrees).</summary>
        public float FieldOfView { get; set; }

        /// <summary>Viewport aspect ratio (width / height, default: 16/9).</summary>
        public float AspectRatio { get; set; }

        /// <summary>Near clipping plane distance (default: 0.1f).</summary>
        public float NearPlane { get; set; }

        /// <summary>Far clipping plane distance (default: 1000f).</summary>
        public float FarPlane { get; set; }

        /// <summary>Effective camera position after screen shake displacement.</summary>
        public Vector3 EffectivePosition { get; }

        /// <summary>Effective camera target after screen shake displacement.</summary>
        public Vector3 EffectiveTarget { get; }

        /// <summary>Effective camera up vector after screen shake rotation.</summary>
        public Vector3 EffectiveUp { get; }

        /// <summary>Active View matrix including screen shake transformation.</summary>
        public Matrix ViewMatrix { get; }

        /// <summary>Perspective Projection matrix.</summary>
        public Matrix ProjectionMatrix { get; }

        /// <summary>Combined View * Projection matrix.</summary>
        public Matrix ViewProjectionMatrix { get; }

        /// <summary>Un-shaken base View matrix.</summary>
        public Matrix BaseViewMatrix { get; }

        /// <summary>Combined un-shaken BaseViewMatrix * ProjectionMatrix.</summary>
        public Matrix BaseViewProjectionMatrix { get; }

        /// <summary>Current trauma level clamped in [0, 1].</summary>
        public float Trauma { get; set; }

        /// <summary>Trauma decay rate per second (default: 1.5f).</summary>
        public float TraumaDecay { get; set; }

        /// <summary>Maximum shake translation displacement at Trauma = 1.0 (default: 15f).</summary>
        public float MaxShakeTranslation { get; set; }

        /// <summary>Maximum rotational shake in radians at Trauma = 1.0 (default: 0.1f).</summary>
        public float MaxShakeRotation { get; set; }

        /// <summary>Frequency of shake oscillations in Hz (default: 25f).</summary>
        public float ShakeFrequency { get; set; }

        public Camera3D() { }
        public Camera3D(float aspectRatio) { }
        public Camera3D(Viewport viewport) { }
        public Camera3D(Vector3 position, Vector3 target, float aspectRatio = 16f / 9f, float fieldOfView = 0.959931f, float nearPlane = 0.1f, float farPlane = 1000f) { }

        /// <summary>Adds trauma to trigger or intensify screen shake.</summary>
        public void AddTrauma(float amount) { }

        /// <summary>Clears active trauma and resets shake displacement.</summary>
        public void ResetShake() { }

        /// <summary>Decays trauma and updates screen shake displacement.</summary>
        public void Update(GameTime gameTime) { }

        /// <summary>Decays trauma and updates screen shake displacement for delta time.</summary>
        public void Update(float dt) { }

        /// <summary>Focuses camera smoothly on a target with a fixed offset.</summary>
        public void LookAt(Vector3 target, Vector3 offset) { }

        /// <summary>Focuses camera on a target maintaining current offset.</summary>
        public void LookAt(Vector3 target) { }

        /// <summary>Unprojects screen pixel coordinate onto a 3D plane at Z = planeZ.</summary>
        public Vector3 ScreenToWorldPlane(Vector2 screenPos, float planeZ = 0f, Viewport? viewport = null) => Vector3.Zero;

        /// <summary>Unprojects screen pixel coordinate onto Z = planeZ and returns (X, Y).</summary>
        public Vector2 ScreenToWorldPlane2D(Vector2 screenPos, float planeZ = 0f, Viewport? viewport = null) => Vector2.Zero;

        /// <summary>Constructs a 3D Ray passing through screen pixel coordinates.</summary>
        public Ray ScreenPointToRay(Vector2 screenPos, Viewport? viewport = null) => default;

        /// <summary>Intersects screen ray with an arbitrary 3D Plane.</summary>
        public Vector3? ScreenToPlane(Vector2 screenPos, Plane plane, Viewport? viewport = null) => null;

        /// <summary>Projects a 3D world position to 2D screen coordinates.</summary>
        public Vector3 WorldToScreen(Vector3 worldPos, Viewport? viewport = null) => Vector3.Zero;

        /// <summary>Projects a 3D world position to 2D screen coordinates (X, Y).</summary>
        public Vector2 WorldToScreen2D(Vector3 worldPos, Viewport? viewport = null) => Vector2.Zero;
    }

    /// <summary>
    /// IA: Use para exibir textos na tela. Não gere fontes ASCII manuais ou dicionários CharPatterns.
    /// Exemplo: TextRenderer.Draw("Score: " + score, new Vector2(100, 50), Color.White, "ScoreFont");
    /// </summary>
    public static class TextRenderer
    {
        /// <summary>
        /// Draws a text string onto the screen space at the specified coordinates using the default font.
        /// </summary>
        public static void Draw(string text, Vector2 position, Color color) { }

        /// <summary>
        /// Draws a text string onto the screen space at the specified coordinates using a custom compiled font name.
        /// </summary>
        /// <param name="text">The string content to render.</param>
        /// <param name="position">Screen space coordinates.</param>
        /// <param name="color">Text tint color.</param>
        /// <param name="fontName">The registered compiled custom font name (e.g. "ScoreFont"). Defaults to "default".</param>
        public static void Draw(string text, Vector2 position, Color color, string fontName = "default") { }
    }

    /// <summary>
    /// Shadow Keyboard static class providing focus-isolated keyboard states.
    /// </summary>
    public static class Keyboard
    {
        /// <summary>
        /// Retrieves the focus-isolated keyboard state for the current frame.
        /// </summary>
        public static KeyboardState GetState() => default;

        /// <summary>
        /// Sets the active shadow keyboard state.
        /// </summary>
        public static void SetState(KeyboardState state) { }
    }

    /// <summary>
    /// Shadow Mouse static class providing focus-isolated and translated mouse states.
    /// </summary>
    public static class Mouse
    {
        /// <summary>
        /// Retrieves the focus-isolated and translated mouse state for the current frame.
        /// </summary>
        public static MouseState GetState() => default;

        /// <summary>
        /// Sets the active shadow mouse state.
        /// </summary>
        public static void SetState(MouseState state) { }
    }

    /// <summary>
    /// High-performance post-processing manager providing multi-pass bloom, separable Gaussian blur, and ACES Filmic Tone Mapping.
    /// </summary>
    /// <example>
    /// <code>
    /// // Initialization
    /// var postProcess = new PostProcessManager();
    /// postProcess.Initialize(GraphicsDevice, 1280, 720);
    /// postProcess.BloomThreshold = 0.75f;
    /// postProcess.BloomIntensity = 1.2f;
    /// 
    /// // Render Loop
    /// postProcess.BeginCapture(GraphicsDevice, Color.CornflowerBlue);
    /// // ... draw scene / sprites ...
    /// postProcess.EndCapture(GraphicsDevice);
    /// 
    /// postProcess.ApplyBloom(spriteBatch, bloomEffect);
    /// postProcess.DrawToScreen(spriteBatch);
    /// </code>
    /// </example>
    public class PostProcessManager : IDisposable
    {
        /// <summary>Gets or sets whether post-processing is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Luminance threshold above which pixels contribute to bloom (default: 0.75).</summary>
        public float BloomThreshold { get; set; } = 0.75f;

        /// <summary>Bloom glow intensity multiplier (default: 1.2).</summary>
        public float BloomIntensity { get; set; } = 1.2f;

        /// <summary>Bloom blur filter spread radius (default: 1.0).</summary>
        public float BloomRadius { get; set; } = 1.0f;

        /// <summary>Exposure compensation value passed to ACES Filmic tone mapping (default: 1.0).</summary>
        public float Exposure { get; set; } = 1.0f;

        /// <summary>The captured HDR scene render target.</summary>
        public RenderTarget2D? SceneTarget { get; }

        /// <summary>The final composite render target after bloom and tone mapping.</summary>
        public RenderTarget2D? CompositeTarget { get; }

        /// <summary>Active buffer width in pixels.</summary>
        public int Width { get; }

        /// <summary>Active buffer height in pixels.</summary>
        public int Height { get; }

        /// <summary>Gets whether render targets have been allocated.</summary>
        public bool IsInitialized { get; }

        /// <summary>Initializes or resizes render targets to match desired dimensions.</summary>
        public void Initialize(GraphicsDevice graphicsDevice, int width, int height) { }

        /// <summary>Redirects draw calls to the offscreen SceneTarget.</summary>
        public void BeginCapture(GraphicsDevice graphicsDevice, Color? clearColor = null) { }

        /// <summary>Restores the default backbuffer target.</summary>
        public void EndCapture(GraphicsDevice graphicsDevice) { }

        /// <summary>Executes bright pass, mip chain separable Gaussian blur, and ACES tone mapping composite.</summary>
        public void ApplyBloom(SpriteBatch spriteBatch, Effect? bloomEffect = null) { }

        /// <summary>Draws the composite render target to the backbuffer.</summary>
        public void DrawToScreen(SpriteBatch spriteBatch, Rectangle? destinationRectangle = null) { }

        /// <summary>Disposes all allocated render targets.</summary>
        public void Dispose() { }
    }

    /// <summary>
    /// Background color key type for procedural background removal.
    /// </summary>
    public enum BgType
    {
        /// <summary>Pure or near black background (RGB: 0, 0, 0).</summary>
        Black,

        /// <summary>Pure or near white background (RGB: 255, 255, 255).</summary>
        White,

        /// <summary>Custom color key (e.g. Chroma green, Magenta).</summary>
        ColorKey
    }

    /// <summary>
    /// Represents a Physically Based Rendering (PBR) material definition
    /// holding albedo, normal, and emissive maps alongside metallic, roughness, and alpha test properties.
    /// </summary>
    /// <example>
    /// <code>
    /// var material = new PbrMaterial
    /// {
    ///     AlbedoMap = albedoTexture,
    ///     NormalMap = normalTexture,
    ///     EmissiveMap = emissiveTexture,
    ///     Roughness = 0.3f,
    ///     Metallic = 0.8f,
    ///     EmissiveIntensity = 2.0f
    /// };
    /// material.Apply(pbrEffect);
    /// </code>
    /// </example>
    public class PbrMaterial : IDisposable
    {
        /// <summary>Gets or sets the base albedo/diffuse texture map.</summary>
        public Texture2D? AlbedoMap { get; set; }

        /// <summary>Gets or sets the tangent-space normal map texture.</summary>
        public Texture2D? NormalMap { get; set; }

        /// <summary>Gets or sets the emissive map texture highlighting glowing regions.</summary>
        public Texture2D? EmissiveMap { get; set; }

        /// <summary>Gets or sets the base diffuse color tint. Default is (1, 1, 1, 1).</summary>
        public Vector4 DiffuseColor { get; set; } = Vector4.One;

        /// <summary>Gets or sets the tint color for emissive glow. Default is Color.White.</summary>
        public Color EmissiveColor { get; set; } = Color.White;

        /// <summary>Gets or sets the scalar intensity multiplier for emissive glow. Default is 1.0f.</summary>
        public float EmissiveIntensity { get; set; } = 1.0f;

        /// <summary>Gets or sets the surface roughness parameter [0.0 = smooth mirror, 1.0 = completely matte]. Default is 0.5f.</summary>
        public float Roughness { get; set; } = 0.5f;

        /// <summary>Gets or sets the surface metallic parameter [0.0 = dielectric, 1.0 = pure metal]. Default is 0.0f.</summary>
        public float Metallic { get; set; } = 0.0f;

        /// <summary>Gets or sets the alpha cutoff threshold for pixel discard. Default is 0.05f.</summary>
        public float AlphaCutoff { get; set; } = 0.05f;

        /// <summary>Gets or sets whether this material owns the lifecycle of its textures and should dispose them upon Dispose(). Default is true.</summary>
        public bool OwnsTextures { get; set; } = true;

        /// <summary>Applies the material parameters and textures to the target shader Effect.</summary>
        /// <param name="effect">The shader effect to configure.</param>
        public void Apply(Effect? effect) { }

        /// <summary>Disposes of any textures owned by this material.</summary>
        public void Dispose() { }
    }

    /// <summary>
    /// Static utility for runtime procedural texture processing, including background removal,
    /// normal map generation using Sobel filtering, emissive map extraction, and automated PBR material synthesis.
    /// </summary>
    /// <example>
    /// <code>
    /// // Remove black background from sprite
    /// Texture2D transparentSprite = TextureProcessor.RemoveBackground(rawSprite, BgType.Black, 0.1f);
    /// 
    /// // Generate normal map
    /// Texture2D normalMap = TextureProcessor.GenerateNormalMap(transparentSprite, 2.5f);
    /// 
    /// // One-line PBR material creation from file
    /// PbrMaterial material = TextureProcessor.CreatePbrMaterial(GraphicsDevice, "Assets/boss.png", normalStrength: 2.0f, removeBg: true);
    /// </code>
    /// </example>
    public static class TextureProcessor
    {
        /// <summary>Removes background color from a source texture with smooth anti-aliased alpha edge falloff.</summary>
        public static Texture2D RemoveBackground(Texture2D source, BgType type, float threshold = 0.1f, Color? customKey = null) => null!;

        /// <summary>Processes raw pixel colors to key out a background color with smooth anti-aliased alpha edge falloff (headless/Zero-GC).</summary>
        public static void ProcessRemoveBackground(ReadOnlySpan<Color> source, Span<Color> destination, int width, int height, BgType type, float threshold = 0.1f, Color? customKey = null) { }

        /// <summary>Generates a 3D tangent-space normal map from a source texture using a 3x3 Sobel filter on pixel luminance.</summary>
        public static Texture2D GenerateNormalMap(Texture2D source, float strength = 2.0f) => null!;

        /// <summary>Generates a 3D tangent-space normal map from raw pixel colors using a 3x3 Sobel filter on pixel luminance (headless/Zero-GC).</summary>
        public static void ProcessNormalMap(ReadOnlySpan<Color> source, Span<Color> destination, int width, int height, float strength = 2.0f) { }

        /// <summary>Analyzes HSV saturation and brightness to isolate neon/intense colors into an emissive mask texture.</summary>
        public static Texture2D GenerateEmissiveMap(Texture2D source, float threshold = 0.65f, float saturationMin = 0.4f) => null!;

        /// <summary>Analyzes HSV saturation and brightness from raw pixel colors to isolate neon/intense emissive areas (headless/Zero-GC).</summary>
        public static void ProcessEmissiveMap(ReadOnlySpan<Color> source, Span<Color> destination, int width, int height, float threshold = 0.65f, float saturationMin = 0.4f) { }

        /// <summary>Loads an image from disk, optionally removes its background, generates normal and emissive maps, and returns a fully initialized PbrMaterial.</summary>
        public static PbrMaterial CreatePbrMaterial(GraphicsDevice graphicsDevice, string filePath, float normalStrength = 2.0f, float emissiveThreshold = 0.65f, BgType bgType = BgType.Black, bool removeBg = false) => null!;

        /// <summary>Procedurally generates normal and emissive maps for an existing texture and constructs a ready-to-use PbrMaterial.</summary>
        public static PbrMaterial CreatePbrMaterial(Texture2D source, float normalStrength = 2.0f, float emissiveThreshold = 0.65f, BgType bgType = BgType.Black, bool removeBg = false) => null!;
    }

    /// <summary>
    /// Represents an individual 3D particle state in world space.
    /// Memory layout is tightly packed to avoid GC allocations during lifecycle updates.
    /// </summary>
    public struct Particle3D
    {
        /// <summary>3D position of the particle in world space.</summary>
        public Vector3 Position;

        /// <summary>3D linear velocity vector (units per second).</summary>
        public Vector3 Velocity;

        /// <summary>3D constant linear acceleration (e.g. gravity, wind, propulsion).</summary>
        public Vector3 Acceleration;

        /// <summary>Color tint at spawn time (t = 0).</summary>
        public Color Color;

        /// <summary>Color tint at death time (t = MaxLife).</summary>
        public Color EndColor;

        /// <summary>Billboard size/diameter at spawn time (world units or projected pixels).</summary>
        public float Size;

        /// <summary>Billboard size/diameter at death time.</summary>
        public float EndSize;

        /// <summary>Current planar rotation angle in radians around the billboard normal.</summary>
        public float Rotation;

        /// <summary>Angular velocity in radians per second.</summary>
        public float RotationSpeed;

        /// <summary>Remaining lifetime in seconds.</summary>
        public float Life;

        /// <summary>Initial total lifetime in seconds.</summary>
        public float MaxLife;

        /// <summary>Indicates whether this particle slot is currently active and rendered.</summary>
        public bool Active;

        /// <summary>Gets the normalized progression of particle life [0.0 = spawn, 1.0 = death].</summary>
        public readonly float NormalizedProgress => 0f;

        /// <summary>Gets the current interpolated color between Color and EndColor based on lifetime progress.</summary>
        public readonly Color CurrentColor => default;

        /// <summary>Gets the current interpolated size between Size and EndSize based on lifetime progress.</summary>
        public readonly float CurrentSize => 0f;
    }

    /// <summary>
    /// High-performance 3D/2.5D Particle System with fixed-size pre-allocated particle pools
    /// ensuring Zero GC memory allocation during frame Updates and Draws.
    /// Supports out-of-the-box presets, additive/alpha blending, and both SpriteBatch 2D projection and 3D Billboard Quad rendering.
    /// </summary>
    /// <example>
    /// <code>
    /// // Instantiate pool
    /// var particles = new ParticleSystem3D(2048);
    /// particles.Gravity = new Vector3(0, -50f, 0);
    /// 
    /// // Emit effects
    /// particles.EmitExplosion(impactPos, count: 60, speed: 220f);
    /// particles.EmitSparks(sparkPos, hitNormal, spreadAngleDegrees: 35f, count: 25);
    /// particles.EmitThrusterTrail(enginePos, -shipDir, speed: 150f);
    /// 
    /// // Update & Draw
    /// particles.Update(gameTime);
    /// particles.Draw(spriteBatch, camera, blendState: BlendState.Additive);
    /// </code>
    /// </example>
    public class ParticleSystem3D : IDisposable
    {
        /// <summary>Default particle pool capacity.</summary>
        public const int DefaultMaxParticles = 2048;

        /// <summary>Gets the maximum capacity of the particle pool.</summary>
        public int MaxParticles { get; } = 2048;

        /// <summary>Gets the number of currently active particles.</summary>
        public int ActiveParticlesCount { get; } = 0;

        /// <summary>Optional global gravity acceleration vector applied to all active particles during update. Default is Vector3.Zero.</summary>
        public Vector3 Gravity { get; set; } = Vector3.Zero;

        /// <summary>Linear velocity damping coefficient [0.0 = no drag, 1.0 = instant stop per second]. Default is 0.0f.</summary>
        public float Drag { get; set; } = 0.0f;

        /// <summary>Initializes a new instance of ParticleSystem3D with the specified maximum particle pool capacity.</summary>
        public ParticleSystem3D(int maxParticles = DefaultMaxParticles) { }

        /// <summary>Emits a single particle into the system pool.</summary>
        public bool Emit(in Particle3D particle) => false;

        /// <summary>Emits a customized particle with explicit kinematic and aesthetic parameters.</summary>
        public bool EmitCustom(Vector3 position, Vector3 velocity, Vector3 acceleration, Color startColor, Color endColor, float startSize, float endSize, float lifetime, float rotation = 0f, float rotationSpeed = 0f) => false;

        /// <summary>Emits a multi-stage explosive burst with bright burning core particles, high-velocity sparks, and rising smoke embers.</summary>
        public void EmitExplosion(Vector3 position, int count = 50, Color? startColor = null, Color? endColor = null, float speed = 200f) { }

        /// <summary>Emits a directional spray of fast sparks spreading in a cone around a target trajectory.</summary>
        public void EmitSparks(Vector3 position, Vector3 direction, float spreadAngleDegrees = 30f, int count = 25, Color? color = null) { }

        /// <summary>Emits an expanding circular shockwave ring centered at a world position.</summary>
        public void EmitShockwave(Vector3 position, float radius = 50f, Color? color = null, float expansionSpeed = 300f) { }

        /// <summary>Emits an engine thruster trail plume trailing behind a moving spacecraft or projectile.</summary>
        public void EmitThrusterTrail(Vector3 position, Vector3 backwardDir, float speed = 100f, Color? flameColor = null) { }

        /// <summary>Updates the kinematics, rotations, lifetimes, and color/size interpolations of all active particles with zero GC allocations.</summary>
        public void Update(GameTime gameTime) { }

        /// <summary>Projects 3D particles onto the 2D screen viewport and renders them using SpriteBatch.</summary>
        public void Draw(SpriteBatch spriteBatch, Camera3D camera, Texture2D? particleTexture = null, BlendState? blendState = null) { }

        /// <summary>Renders active particles as camera-aligned 3D billboard quads directly on the GPU using BasicEffect.</summary>
        public void Draw3D(GraphicsDevice graphicsDevice, Camera3D camera, Texture2D? particleTexture = null, BlendState? blendState = null) { }

        /// <summary>Clears all currently active particles and resets the system pool.</summary>
        public void Clear() { }

        /// <summary>Releases all managed and unmanaged GPU resources used by the particle system.</summary>
        public void Dispose() { }
    }
}
