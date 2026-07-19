using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    public class EntityManager
    {
        private readonly List<GameEntity> _entities = new List<GameEntity>();

        public IReadOnlyList<GameEntity> Entities => _entities;

        public void AddEntity(GameEntity entity)
        {
            if (entity != null && !_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
        }

        public void RemoveEntity(GameEntity entity)
        {
            if (entity != null)
            {
                _entities.Remove(entity);
            }
        }

        public void UpdateAll(GameTime gameTime)
        {
            // Create a copy to prevent modification issues during updates
            var temp = new List<GameEntity>(_entities);
            foreach (var entity in temp)
            {
                entity.Update(gameTime);
            }
        }

        public void DrawAll(SpriteBatch spriteBatch)
        {
            foreach (var entity in _entities)
            {
                entity.Draw(spriteBatch);
            }
        }
    }
}
