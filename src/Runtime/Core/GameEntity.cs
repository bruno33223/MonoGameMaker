using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    public class GameEntity
    {
        public Guid Id { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }

        public GameEntity()
        {
            Id = Guid.NewGuid();
        }

        public GameEntity(Guid id)
        {
            Id = id;
        }

        public void LoadState(Guid id)
        {
            Id = id;
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
}
