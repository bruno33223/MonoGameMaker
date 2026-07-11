using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.IDE.Core
{
    public abstract class Component
    {
        public SceneSerializer.EntityInstance? Parent { get; set; }
        public bool Enabled { get; set; } = true;

        public virtual void Awake() { }
        public virtual void Start() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
    }
}
