using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Base game entity class supporting transformation, lifecycle state, heading offset orientation, and rendering hooks.
    /// </summary>
    public class GameEntity
    {
        public Guid Id { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }

        /// <summary>
        /// Angular offset in radians added to the heading/rotation so sprites generated in different
        /// default orientations (e.g., horizontal/facing right) can be cleanly aligned with standard physics.
        /// </summary>
        public float HeadingOffset { get; set; } = 0f;

        /// <summary>
        /// Gets or sets whether this entity is dead/destroyed and should be swept from the EntityManager.
        /// </summary>
        public bool IsDead { get; set; } = false;

        /// <summary>
        /// Gets or sets whether this entity is active. Inactive entities do not update or draw.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Optional tag identifier for filtering and lookups.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Rendering/layer order index.
        /// </summary>
        public int Layer { get; set; } = 0;

        /// <summary>
        /// Instância ativa do comportamento (script) atrelado a esta entidade.
        /// </summary>
        public EntityBehavior? Script { get; set; }

        /// <summary>
        /// Dicionário de propriedades configuradas via Prefab/Cena.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> Properties { get; set; } = new System.Collections.Generic.Dictionary<string, string>();

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

        /// <summary>
        /// Marks this entity as dead so it is automatically removed and cleaned up during the EntityManager's sweep phase.
        /// </summary>
        public virtual void Destroy()
        {
            if (IsDead) return;
            IsDead = true;
            OnDestroyed();
        }

        /// <summary>
        /// Virtual lifecycle callback invoked when <see cref="Destroy"/> is called.
        /// </summary>
        public virtual void OnDestroyed()
        {
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
}
