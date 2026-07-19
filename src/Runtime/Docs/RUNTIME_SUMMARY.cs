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
        /// <summary>
        /// Reference to the GameEntity holding this behavior.
        /// </summary>
        public GameEntity Entity { get; }

        /// <summary>
        /// Key-value properties initialized from the Prefab configuration.
        /// </summary>
        public Dictionary<string, string> Properties { get; }

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
        /// The name of the prefab this entity was instantiated from.
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// The active 2D Sprite Texture of this entity.
        /// </summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// The current position coordinate of this entity in World Space.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The active behavior script attached to this entity, if any.
        /// </summary>
        public EntityBehavior Script { get; set; }

        /// <summary>
        /// Tag classification category used for query lookups and collision filtering.
        /// </summary>
        public string Tag { get; set; }

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
        public static List<GameEntity> Entities { get; }

        /// <summary>
        /// Spawns a new entity instance from a prefab configuration.
        /// </summary>
        /// <example>
        /// <code>
        /// var player = EntityManager.Spawn("Player", new Vector2(100, 100));
        /// </code>
        /// </example>
        public static GameEntity Spawn(string prefabName, Vector2 position) => null;

        /// <summary>
        /// Queues the specified entity for immediate destruction at the end of the frame.
        /// </summary>
        public static void Destroy(GameEntity entity) { }

        /// <summary>
        /// Queries the world to find the first entity carrying a specific tag that overlaps the caller's bounding box.
        /// </summary>
        public static GameEntity GetFirstColliding(GameEntity caller, string targetTag) => null;

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
        public static Dictionary<string, object> Data { get; }

        /// <summary>
        /// Sets a persistent global value.
        /// </summary>
        public static void Set(string key, object value) { }

        /// <summary>
        /// Retrieves a persistent global value, with a type-fallback.
        /// </summary>
        public static T Get<T>(string key) => default;

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
        public static string CurrentScene { get; }

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
}
