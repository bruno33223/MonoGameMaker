using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Classe base para comportamentos modulares de entidade no MonoGameMaker.
    /// Expõe os hooks de ciclo de vida (Awake, Update, Draw, DrawUI, OnCollision, Dispose)
    /// e integra com o EntityManager e o Inspector com Reflection do editor.
    /// </summary>
    public abstract class EntityBehavior : IDisposable
    {
        /// <summary>
        /// Referência à GameEntity proprietária deste comportamento.
        /// </summary>
        public GameEntity Entity { get; internal set; } = null!;

        /// <summary>
        /// Dicionário de propriedades configuradas via Prefab/Cena.
        /// </summary>
        public Dictionary<string, string> Properties { get; internal set; } = new Dictionary<string, string>();

        /// <summary>
        /// Chamado imediatamente após a instanciação e vinculação da entidade.
        /// </summary>
        public virtual void Awake() { }

        /// <summary>
        /// Chamado a cada quadro de atualização da simulação.
        /// </summary>
        public virtual void Update(GameTime gameTime) { }

        /// <summary>
        /// Chamado no passo de desenho principal via SpriteBatch.
        /// </summary>
        public virtual void Draw(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Chamado no passo de desenho de interface com passagem de SpriteBatch.
        /// Por padrão delega para o DrawUI() sem parâmetros.
        /// </summary>
        public virtual void DrawUI(SpriteBatch spriteBatch)
        {
            DrawUI();
        }

        /// <summary>
        /// Chamado no passo de desenho de interface imediata (ex: ImGui).
        /// </summary>
        public virtual void DrawUI() { }

        /// <summary>
        /// Chamado pelo sistema de colisão quando esta entidade interage com outra entidade.
        /// </summary>
        public virtual void OnCollision(GameEntity other) { }

        /// <summary>
        /// Libera inscrições de eventos globais e recursos gerenciados.
        /// </summary>
        public virtual void Dispose() { }
    }
}
