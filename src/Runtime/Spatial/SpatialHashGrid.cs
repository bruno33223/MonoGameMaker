using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Spatial
{
    /// <summary>
    /// Particionamento espacial 2D de alta performance totalmente Zero-GC
    /// baseado em Static Linked Arrays para simulação de multidões e seleções em lote.
    /// </summary>
    public sealed class SpatialHashGrid
    {
        private readonly int _cellColumns;
        private readonly int _cellRows;
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly int _worldWidth;
        private readonly int _worldHeight;
        private readonly int _maxEntities;

        // Estrutura Static Linked Array (Zero-GC)
        private readonly int[] _cellHead;    // Tamanho: cellColumns * cellRows
        private readonly int[] _nextEntity;  // Tamanho: maxEntities
        private readonly int[] _entityCell;  // Tamanho: maxEntities

        // Cache de posições para consultas sem dereferenciamento indireto
        private readonly Vector2[] _positions;

        public int CellColumns => _cellColumns;
        public int CellRows => _cellRows;
        public float CellSize => _cellSize;
        public int MaxEntities => _maxEntities;
        public ReadOnlySpan<Vector2> Positions => _positions;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCellHead(int cellIndex) =>
            (uint)cellIndex < (uint)_cellHead.Length ? _cellHead[cellIndex] : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNextEntity(int entityId) =>
            (uint)entityId < (uint)_maxEntities ? _nextEntity[entityId] : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityCell(int entityId) =>
            (uint)entityId < (uint)_maxEntities ? _entityCell[entityId] : -1;

        public SpatialHashGrid(int worldWidth, int worldHeight, float cellSize, int maxEntities = 2048)
        {
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _cellSize = cellSize;
            _invCellSize = 1.0f / cellSize;
            _maxEntities = maxEntities;

            _cellColumns = (int)MathF.Ceiling(worldWidth * _invCellSize);
            _cellRows = (int)MathF.Ceiling(worldHeight * _invCellSize);

            int totalCells = _cellColumns * _cellRows;
            _cellHead = new int[totalCells];
            _nextEntity = new int[maxEntities];
            _entityCell = new int[maxEntities];
            _positions = new Vector2[maxEntities];

            Clear();
        }

        /// <summary>
        /// Limpa os cabeçalhos de células em O(C) via Array.Fill (memset nativo).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Fill(_cellHead, -1);
            Array.Fill(_nextEntity, -1);
            Array.Fill(_entityCell, -1);
        }

        /// <summary>
        /// Insere uma entidade na célula correspondente à sua posição em O(1).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int entityId, Vector2 position)
        {
            if ((uint)entityId >= (uint)_maxEntities) return;

            // Se a entidade já estiver indexada em alguma célula, remove antes de reinserir
            if (_entityCell[entityId] != -1)
            {
                Remove(entityId);
            }

            int cx = Math.Clamp((int)(position.X * _invCellSize), 0, _cellColumns - 1);
            int cy = Math.Clamp((int)(position.Y * _invCellSize), 0, _cellRows - 1);
            int cellIndex = cy * _cellColumns + cx;

            _positions[entityId] = position;
            _nextEntity[entityId] = _cellHead[cellIndex];
            _cellHead[cellIndex] = entityId;
            _entityCell[entityId] = cellIndex;
        }

        /// <summary>
        /// Remove uma entidade de sua célula atual reconectando os ponteiros da lista encadeada.
        /// Operação O(k) onde k é o número de entidades na mesma célula.
        /// </summary>
        public void Remove(int entityId)
        {
            if ((uint)entityId >= (uint)_maxEntities) return;

            int cellIndex = _entityCell[entityId];
            if (cellIndex < 0) return; // Não inserida

            int current = _cellHead[cellIndex];
            int prev = -1;

            while (current != -1)
            {
                if (current == entityId)
                {
                    if (prev == -1)
                        _cellHead[cellIndex] = _nextEntity[entityId];
                    else
                        _nextEntity[prev] = _nextEntity[entityId];

                    _nextEntity[entityId] = -1;
                    _entityCell[entityId] = -1;
                    return;
                }
                prev = current;
                current = _nextEntity[current];
            }

            _nextEntity[entityId] = -1;
            _entityCell[entityId] = -1;
        }

        /// <summary>
        /// Atualiza a posição de uma entidade. Se ela permaneceu na mesma célula,
        /// apenas atualiza o vetor de cache. Caso tenha cruzado fronteiras, faz Remove e Insert.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePosition(int entityId, Vector2 oldPos, Vector2 newPos)
        {
            if ((uint)entityId >= (uint)_maxEntities) return;

            int oldCx = Math.Clamp((int)(oldPos.X * _invCellSize), 0, _cellColumns - 1);
            int oldCy = Math.Clamp((int)(oldPos.Y * _invCellSize), 0, _cellRows - 1);
            int newCx = Math.Clamp((int)(newPos.X * _invCellSize), 0, _cellColumns - 1);
            int newCy = Math.Clamp((int)(newPos.Y * _invCellSize), 0, _cellRows - 1);

            _positions[entityId] = newPos;

            if (oldCx == newCx && oldCy == newCy && _entityCell[entityId] != -1)
            {
                return; // Mesma célula, zero reconexão de ponteiros necessária!
            }

            Remove(entityId);
            Insert(entityId, newPos);
        }

        /// <summary>
        /// Localiza entidades vizinhas dentro de um raio euclidiano R.
        /// Grava os IDs diretamente no Span fornecido sem qualquer alocação de Heap.
        /// </summary>
        public int FindNeighbors(Vector2 center, float radius, Span<int> results, int ignoreEntityId = -1)
        {
            float radiusSq = radius * radius;
            int minCx = Math.Max(0, (int)((center.X - radius) * _invCellSize));
            int maxCx = Math.Min(_cellColumns - 1, (int)((center.X + radius) * _invCellSize));
            int minCy = Math.Max(0, (int)((center.Y - radius) * _invCellSize));
            int maxCy = Math.Min(_cellRows - 1, (int)((center.Y + radius) * _invCellSize));

            int count = 0;
            int maxResults = results.Length;

            for (int cy = minCy; cy <= maxCy; cy++)
            {
                int rowOffset = cy * _cellColumns;
                for (int cx = minCx; cx <= maxCx; cx++)
                {
                    int current = _cellHead[rowOffset + cx];
                    while (current != -1)
                    {
                        if (current != ignoreEntityId)
                        {
                            Vector2 otherPos = _positions[current];
                            float distSq = Vector2.DistanceSquared(center, otherPos);

                            if (distSq <= radiusSq)
                            {
                                results[count++] = current;
                                if (count >= maxResults) return count;
                            }
                        }
                        current = _nextEntity[current];
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Consulta todas as entidades ativas contidas em uma caixa delimitadora RectangleF (Marquee Box).
        /// </summary>
        public int QueryBox(RectangleF box, Span<int> results)
        {
            int minCx = Math.Max(0, (int)(box.Left * _invCellSize));
            int maxCx = Math.Min(_cellColumns - 1, (int)(box.Right * _invCellSize));
            int minCy = Math.Max(0, (int)(box.Top * _invCellSize));
            int maxCy = Math.Min(_cellRows - 1, (int)(box.Bottom * _invCellSize));

            int count = 0;
            int maxResults = results.Length;

            for (int cy = minCy; cy <= maxCy; cy++)
            {
                int rowOffset = cy * _cellColumns;
                for (int cx = minCx; cx <= maxCx; cx++)
                {
                    int current = _cellHead[rowOffset + cx];
                    while (current != -1)
                    {
                        Vector2 pos = _positions[current];
                        if (box.Contains(pos))
                        {
                            results[count++] = current;
                            if (count >= maxResults) return count;
                        }
                        current = _nextEntity[current];
                    }
                }
            }

            return count;
        }
    }
}
