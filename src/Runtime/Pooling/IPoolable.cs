using System;

namespace MonoGameMaker.Runtime.Pooling
{
    /// <summary>
    /// Contrato estrito para objetos recicláveis em ObjectPools de alta performance com Zero-GC.
    /// Define os hooks de ciclo de vida para inicialização e higienização de estado.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Indica se a instância está atualmente ativa na simulação do jogo.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Chamado imediatamente quando o objeto é retirado do pool para reuso.
        /// Deve reabilitar visibilidade, física e resetar flags essenciais.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Chamado imediatamente quando o objeto é devolvido ao pool.
        /// Deve desabilitar visibilidade, física e limpar referências pendentes.
        /// </summary>
        void OnDespawn();

        /// <summary>
        /// Reseta o estado interno do objeto para os valores de repouso.
        /// </summary>
        void Reset() => OnDespawn();
    }
}
