using System;
using System.Collections.Generic;

namespace MonoGameMaker.Runtime.Pooling
{
    /// <summary>
    /// Gerenciador de Object Pooling genérico, thread-safe e de zero alocações de GC para objetos IPoolable.
    /// Utiliza uma coleção interna LIFO (Stack) para maximizar o reuso de memória e manter instâncias quentes no cache de CPU.
    /// </summary>
    /// <typeparam name="T">Tipo de referência que implementa o contrato IPoolable.</typeparam>
    public class ObjectPool<T> where T : class, IPoolable
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _factory;
        private readonly object _syncLock = new object();

        /// <summary>
        /// Quantidade de instâncias atualmente em repouso no pool (disponíveis para resgate via Get/Rent).
        /// </summary>
        public int Count
        {
            get
            {
                lock (_syncLock)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>
        /// Inicializa um novo ObjectPool com fábrica customizada e capacidade inicial pré-alocada.
        /// </summary>
        /// <param name="factory">Função fábrica para criação de instâncias. Se nula, utiliza Activator.CreateInstance.</param>
        /// <param name="initialCapacity">Capacidade inicial da coleção interna para evitar realocações.</param>
        public ObjectPool(Func<T>? factory = null, int initialCapacity = 64)
        {
            _factory = factory ?? (() => Activator.CreateInstance<T>());
            _pool = new Stack<T>(Math.Max(4, initialCapacity));
        }

        /// <summary>
        /// Pré-aquece o pool instanciando em lote a quantidade solicitada de objetos.
        /// Garante que nenhuma alocação de GC ocorra durante o loop de combate ativo.
        /// </summary>
        /// <param name="count">Número de instâncias a serem pré-alocadas.</param>
        public void PreWarm(int count)
        {
            if (count <= 0) return;

            lock (_syncLock)
            {
                for (int i = 0; i < count; i++)
                {
                    T item = _factory();
                    item.IsActive = false;
                    item.OnDespawn();
                    _pool.Push(item);
                }
            }
        }

        /// <summary>
        /// Obtém uma instância pronta para uso do pool. Executa OnSpawn() imediatamente.
        /// Caso o pool esteja vazio, instancia uma nova entidade via fábrica.
        /// </summary>
        /// <returns>Instância reciclada de T com estado pronto para simulação.</returns>
        public T Get()
        {
            T item;
            lock (_syncLock)
            {
                item = _pool.Count > 0 ? _pool.Pop() : _factory();
            }

            item.IsActive = true;
            item.OnSpawn();
            return item;
        }

        /// <summary>
        /// Alias idiomático de alta performance para <see cref="Get"/>.
        /// </summary>
        public T Rent() => Get();

        /// <summary>
        /// Devolve uma instância utilizada ao pool. Executa OnDespawn() para higienização e reciclagem.
        /// </summary>
        /// <param name="obj">Instância a ser devolvida ao pool.</param>
        public void Release(T obj)
        {
            if (obj == null) return;

            obj.IsActive = false;
            obj.OnDespawn();

            lock (_syncLock)
            {
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Alias idiomático de alta performance para <see cref="Release"/>.
        /// </summary>
        public void Return(T obj) => Release(obj);

        /// <summary>
        /// Limpa todas as instâncias em repouso no pool.
        /// </summary>
        public void Clear()
        {
            lock (_syncLock)
            {
                _pool.Clear();
            }
        }
    }
}
