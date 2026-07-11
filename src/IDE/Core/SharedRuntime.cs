using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public abstract class EntityBehavior
    {
        public GameEntity Entity { get; internal set; } = null!;
        public Dictionary<string, string> Properties { get; internal set; } = null!;

        public virtual void Awake() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnCollision(GameEntity other) { }
    }

    public class GameEntity
    {
        public string PrefabName { get; set; } = string.Empty;
        public Texture2D? Texture { get; set; }
        public Vector2 Position { get; set; }
        public EntityBehavior? Script { get; set; }
        public string Tag { get; set; } = "Default";
        public bool IsDestroyed { get; set; } = false;
        
        public Rectangle? SourceRect { get; set; } = null;

        private int _animFrameWidth;
        private int _animFrameHeight;
        private int _animStartFrame;
        private int _animEndFrame;
        private float _animFps;
        private double _animTimer;
        private int _animCurrentFrame;
        private bool _isAnimating;

        public Rectangle Bounds => SourceRect.HasValue
            ? new Rectangle((int)Position.X, (int)Position.Y, SourceRect.Value.Width, SourceRect.Value.Height)
            : (Texture != null 
                ? new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height) 
                : new Rectangle((int)Position.X, (int)Position.Y, 64, 64));

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

    public class RuntimeScene
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public Color BackgroundColor { get; set; } = Color.CornflowerBlue;
        public Texture2D? BackgroundImage { get; set; }
        public List<GameEntity> Entities { get; set; } = new List<GameEntity>();
    }

    public class SceneData
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;
        public System.Numerics.Vector3 BackgroundColor { get; set; } = new System.Numerics.Vector3(0.1f, 0.1f, 0.2f);
        public string BackgroundImage { get; set; } = string.Empty;
        public List<EntityInstance> Instances { get; set; } = new List<EntityInstance>();
    }

    public class EntityInstance
    {
        public string prefabName { get; set; } = string.Empty;
        public float x { get; set; }
        public float y { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    public class PrefabData
    {
        public string TextureName { get; set; } = string.Empty;
        public string ScriptName { get; set; } = string.Empty;
        public string Tag { get; set; } = "Default";
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }
}
