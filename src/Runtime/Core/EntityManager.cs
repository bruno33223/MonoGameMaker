using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Manages the lifecycle, two-phase update, dead entity sweeping, and drawing of all GameEntity instances.
    /// </summary>
    public class EntityManager
    {
        private readonly List<GameEntity> _entities = new List<GameEntity>();
        private readonly List<GameEntity> _pendingAdd = new List<GameEntity>();
        private bool _isUpdating = false;

        /// <summary>
        /// Gets the read-only list of active entities.
        /// </summary>
        public IReadOnlyList<GameEntity> Entities => _entities;

        /// <summary>
        /// Gets the current number of registered entities.
        /// </summary>
        public int Count => _entities.Count;

        /// <summary>
        /// Adds an entity to the manager. If called during an Update cycle, the entity is queued and added cleanly in Phase 2.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public void AddEntity(GameEntity entity)
        {
            if (entity == null) return;

            if (_isUpdating)
            {
                if (!_pendingAdd.Contains(entity) && !_entities.Contains(entity))
                {
                    _pendingAdd.Add(entity);
                }
            }
            else
            {
                if (!_entities.Contains(entity))
                {
                    _entities.Add(entity);
                }
            }
        }

        /// <summary>
        /// Creates and registers a new default <see cref="GameEntity"/>.
        /// </summary>
        public GameEntity CreateEntity()
        {
            var entity = new GameEntity();
            AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Creates and registers a typed instance of <see cref="GameEntity"/>.
        /// </summary>
        public T CreateEntity<T>() where T : GameEntity, new()
        {
            var entity = new T();
            AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Restores and registers an entity with a specific Guid identifier.
        /// </summary>
        public GameEntity RestoreEntity(Guid id)
        {
            var entity = new GameEntity(id);
            AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Removes an entity immediately and marks it as dead.
        /// </summary>
        public void RemoveEntity(GameEntity entity)
        {
            if (entity != null)
            {
                entity.Destroy();
                _pendingAdd.Remove(entity);
                _entities.Remove(entity);
            }
        }

        /// <summary>
        /// Clears all entities, marking them as dead.
        /// </summary>
        public void Clear()
        {
            _pendingAdd.Clear();
            for (int i = 0; i < _entities.Count; i++)
            {
                _entities[i].Destroy();
            }
            _entities.Clear();
        }

        /// <summary>
        /// Executes an automatic two-phase update and sweep:
        /// Phase 1 (Update): Updates all alive and active entities.
        /// Phase 2 (Sweep): Automatically purges dead entities (IsDead == true) and adds queued entities.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _isUpdating = true;
            try
            {
                // Phase 1: Update all alive & active entities
                int initialCount = _entities.Count;
                for (int i = 0; i < initialCount; i++)
                {
                    var entity = _entities[i];
                    if (!entity.IsDead && entity.Active)
                    {
                        entity.Update(gameTime);
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }

            // Phase 2: Sweep dead entities and flush pending additions
            SweepDeadEntities();

            if (_pendingAdd.Count > 0)
            {
                for (int i = 0; i < _pendingAdd.Count; i++)
                {
                    var entity = _pendingAdd[i];
                    if (!entity.IsDead && !_entities.Contains(entity))
                    {
                        _entities.Add(entity);
                    }
                }
                _pendingAdd.Clear();
            }
        }

        /// <summary>
        /// Backward-compatible alias for Update.
        /// </summary>
        public void UpdateAll(GameTime gameTime)
        {
            Update(gameTime);
        }

        /// <summary>
        /// Purges all entities where <see cref="GameEntity.IsDead"/> is true.
        /// </summary>
        public void SweepDeadEntities()
        {
            _entities.RemoveAll(e => e == null || e.IsDead);
        }

        /// <summary>
        /// Draws all alive and active entities.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                var entity = _entities[i];
                if (!entity.IsDead && entity.Active)
                {
                    entity.Draw(spriteBatch);
                }
            }
        }

        /// <summary>
        /// Backward-compatible alias for Draw.
        /// </summary>
        public void DrawAll(SpriteBatch spriteBatch)
        {
            Draw(spriteBatch);
        }

        /// <summary>
        /// Retrieves all alive entities of a specific type.
        /// </summary>
        public IEnumerable<T> GetEntities<T>() where T : GameEntity
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i] is T typed && !typed.IsDead)
                {
                    yield return typed;
                }
            }
        }

        /// <summary>
        /// Finds an alive entity matching the specified identifier.
        /// </summary>
        public GameEntity? FindEntity(Guid id)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Id == id && !_entities[i].IsDead)
                {
                    return _entities[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Finds all alive entities matching a specified tag.
        /// </summary>
        public List<GameEntity> FindByTag(string tag)
        {
            var results = new List<GameEntity>();
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (!e.IsDead && string.Equals(e.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(e);
                }
            }
            return results;
        }
    }
}
