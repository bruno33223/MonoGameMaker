using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime
{
    public interface IEntityScript
    {
        void Initialize(GameEntity entity, Dictionary<string, string> properties);
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
        void DrawUI(SpriteBatch spriteBatch);
    }

    public class GameEntity
    {
        public string PrefabName { get; set; } = string.Empty;
        public Texture2D? Texture { get; set; }
        public Vector2 Position { get; set; }
        public IEntityScript? Script { get; set; }
        public string Tag { get; set; } = "Default";
        public bool IsDestroyed { get; set; } = false;
        public Rectangle Bounds => Texture != null 
            ? new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height) 
            : new Rectangle((int)Position.X, (int)Position.Y, 64, 64);
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
