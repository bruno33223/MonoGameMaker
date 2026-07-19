using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    public abstract class EntityBehavior : IDisposable
    {
        /// <summary>
        /// Reference to the GameEntity holding this behavior.
        /// </summary>
        public GameEntity Entity { get; internal set; } = null!;

        /// <summary>
        /// Key-value properties initialized from the Prefab configuration.
        /// </summary>
        public Dictionary<string, string> Properties { get; internal set; } = null!;

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
        public virtual void DrawUI(SpriteBatch spriteBatch)
        {
            DrawUI();
        }

        /// <summary>
        /// Called once per frame to draw screen-space UI elements. Override this method to perform direct ImGui calls (e.g. ImGui.Begin, ImGui.Button).
        /// </summary>
        public virtual void DrawUI() { }

        /// <summary>
        /// Called dynamically when this entity collides with another entity.
        /// </summary>
        public virtual void OnCollision(GameEntity other) { }

        /// <summary>
        /// Disposes of any resources (such as event subscriptions) held by the behavior.
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
        public string PrefabName { get; set; } = string.Empty;

        /// <summary>
        /// The active 2D Sprite Texture of this entity.
        /// </summary>
        public Texture2D? Texture { get; set; }

        /// <summary>
        /// The current position coordinate of this entity in World Space.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The active behavior script attached to this entity, if any.
        /// </summary>
        public EntityBehavior? Script { get; set; }

        /// <summary>
        /// Tag classification category used for query lookups and collision filtering.
        /// </summary>
        public string Tag { get; set; } = "Default";

        /// <summary>
        /// True if the entity is queued for deletion.
        /// </summary>
        public bool IsDestroyed { get; set; } = false;
        
        /// <summary>
        /// The active sprite clip viewport sheet rectangle (null if full texture is drawn).
        /// </summary>
        public Rectangle? SourceRect { get; set; } = null;

        /// <summary>
        /// Custom offset coordinates for the collision hitbox relative to the position coordinate.
        /// </summary>
        public Vector2 HitboxOffset { get; set; } = Vector2.Zero;

        /// <summary>
        /// Custom dimensions for the collision hitbox.
        /// </summary>
        public Vector2 HitboxSize { get; set; } = Vector2.Zero;

        private int _animFrameWidth;
        private int _animFrameHeight;
        private int _animStartFrame;
        private int _animEndFrame;
        private float _animFps;
        private double _animTimer;
        private int _animCurrentFrame;
        private bool _isAnimating;

        /// <summary>
        /// Computes the final World Space axis-aligned bounding box (AABB) of the entity's collision mask.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                int w = HitboxSize.X > 0f ? (int)HitboxSize.X : (SourceRect.HasValue ? SourceRect.Value.Width : (Texture != null ? Texture.Width : 64));
                int h = HitboxSize.Y > 0f ? (int)HitboxSize.Y : (SourceRect.HasValue ? SourceRect.Value.Height : (Texture != null ? Texture.Height : 64));
                return new Rectangle((int)(Position.X + HitboxOffset.X), (int)(Position.Y + HitboxOffset.Y), w, h);
            }
        }

        /// <summary>
        /// Starts playing a uniform grid-based spritesheet animation clip.
        /// </summary>
        public void PlayAnimation(int frameWidth, int frameHeight, int startFrame, int endFrame, float fps)
        {
            if (_animFrameWidth == frameWidth && _animFrameHeight == frameHeight &&
                _animStartFrame == startFrame && _animEndFrame == endFrame && _animFps == fps && _isAnimating)
            {
                return;
            }

            _animFrameWidth = frameWidth;
            _animFrameHeight = frameHeight;
            _animStartFrame = startFrame;
            _animEndFrame = endFrame;
            _animFps = fps;
            _animTimer = 0.0;
            _animCurrentFrame = startFrame;
            _isAnimating = true;

            UpdateSourceRect();
        }

        /// <summary>
        /// Updates the active animation frame based on the elapsed time.
        /// </summary>
        public void UpdateAnimation(GameTime gameTime)
        {
            if (!_isAnimating) return;

            if (_animFps <= 0f)
            {
                _animCurrentFrame = _animStartFrame;
                UpdateSourceRect();
                return;
            }

            _animTimer += gameTime.ElapsedGameTime.TotalSeconds;
            double frameDuration = 1.0 / _animFps;

            if (_animTimer >= frameDuration)
            {
                int framesToAdvance = (int)(_animTimer / frameDuration);
                _animTimer %= frameDuration;

                int frameCount = _animEndFrame - _animStartFrame + 1;
                if (frameCount <= 0) frameCount = 1;

                int localFrameIndex = (_animCurrentFrame - _animStartFrame + framesToAdvance) % frameCount;
                _animCurrentFrame = _animStartFrame + localFrameIndex;

                UpdateSourceRect();
            }
        }

        private void UpdateSourceRect()
        {
            if (Texture == null)
            {
                SourceRect = null;
                return;
            }

            int columns = Texture.Width / _animFrameWidth;
            if (columns <= 0) columns = 1;

            int col = _animCurrentFrame % columns;
            int row = _animCurrentFrame / columns;

            SourceRect = new Rectangle(col * _animFrameWidth, row * _animFrameHeight, _animFrameWidth, _animFrameHeight);
        }
    }

    /// <summary>
    /// Represents loaded scene metrics.
    /// </summary>
    public class RuntimeScene
    {
        /// <summary>
        /// Scene boundary width.
        /// </summary>
        public int Width { get; set; } = 1280;

        /// <summary>
        /// Scene boundary height.
        /// </summary>
        public int Height { get; set; } = 720;

        /// <summary>
        /// Clear color.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.CornflowerBlue;

        /// <summary>
        /// Optional background image texture.
        /// </summary>
        public Texture2D? BackgroundImage { get; set; }

        /// <summary>
        /// List of active entities loaded inside the current scene.
        /// </summary>
        public List<GameEntity> Entities { get; set; } = new List<GameEntity>();
    }

    /// <summary>
    /// Serialized scene properties.
    /// </summary>
    public class SceneData
    {
        /// <summary>
        /// Width boundary.
        /// </summary>
        public int Width { get; set; } = 1280;

        /// <summary>
        /// Height boundary.
        /// </summary>
        public int Height { get; set; } = 720;

        /// <summary>
        /// Background Clear color value vector.
        /// </summary>
        public System.Numerics.Vector3 BackgroundColor { get; set; } = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f);

        /// <summary>
        /// Background image asset name.
        /// </summary>
        public string BackgroundImage { get; set; } = string.Empty;

        /// <summary>
        /// List of initial entity instances.
        /// </summary>
        public List<EntityInstance> Instances { get; set; } = new List<EntityInstance>();
    }

    /// <summary>
    /// Serialized entity instance definition.
    /// </summary>
    public class EntityInstance
    {
        /// <summary>
        /// Target prefab template name.
        /// </summary>
        public string prefabName { get; set; } = string.Empty;

        /// <summary>
        /// Starting horizontal offset.
        /// </summary>
        public float x { get; set; }

        /// <summary>
        /// Starting vertical offset.
        /// </summary>
        public float y { get; set; }

        /// <summary>
        /// Custom configuration parameters.
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Serialized prefab definition.
    /// </summary>
    public class PrefabData
    {
        /// <summary>
        /// Visual texture asset name.
        /// </summary>
        public string TextureName { get; set; } = string.Empty;

        /// <summary>
        /// Behavior script typename.
        /// </summary>
        public string ScriptName { get; set; } = string.Empty;

        /// <summary>
        /// Tag metadata category.
        /// </summary>
        public string Tag { get; set; } = "Default";

        /// <summary>
        /// Hitbox horizontal offset displacement.
        /// </summary>
        public float HitboxOffsetX { get; set; } = 0f;

        /// <summary>
        /// Hitbox vertical offset displacement.
        /// </summary>
        public float HitboxOffsetY { get; set; } = 0f;

        /// <summary>
        /// Hitbox visual width.
        /// </summary>
        public float HitboxWidth { get; set; } = 0f;

        /// <summary>
        /// Hitbox visual height.
        /// </summary>
        public float HitboxHeight { get; set; } = 0f;

        /// <summary>
        /// Custom configuration default parameters.
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }
}
