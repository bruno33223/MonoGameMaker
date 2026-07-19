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

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
}
