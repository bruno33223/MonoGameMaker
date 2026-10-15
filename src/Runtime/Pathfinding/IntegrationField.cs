using System;
using System.Runtime.CompilerServices;

namespace MonoGameMaker.Runtime.Pathfinding
{
    /// <summary>
    /// Expande o gradiente de distância mínima acumulada do alvo para todas as células do mapa
    /// via Wavefront/Dijkstra contíguo sobre uma fila circular estática Zero-GC.
    /// </summary>
    public sealed class IntegrationField
    {
        public const ushort Unvisited = ushort.MaxValue;

        private readonly int _width;
        private readonly int _height;
        private readonly ushort[] _integration;
        private readonly int[] _wavefrontQueue; // Fila circular plana estática (Zero-GC)

        public int Width => _width;
        public int Height => _height;
        public ReadOnlySpan<ushort> RawIntegration => _integration;

        public IntegrationField(int width, int height)
        {
            _width = width;
            _height = height;
            int total = width * height;
            _integration = new ushort[total];
            _wavefrontQueue = new int[total];
            Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => Array.Fill(_integration, Unvisited);

        public void Generate(int goalX, int goalY, CostField costField)
        {
            if ((uint)goalX >= (uint)_width || (uint)goalY >= (uint)_height) return;

            Reset();

            int goalIndex = goalY * _width + goalX;
            _integration[goalIndex] = 0;

            ReadOnlySpan<int> dx = stackalloc int[8] { 0, 0, -1, 1, -1, 1, -1, 1 };
            ReadOnlySpan<int> dy = stackalloc int[8] { -1, 1, 0, 0, -1, -1, 1, 1 };
            ReadOnlySpan<int> dCost = stackalloc int[8] { 10, 10, 10, 10, 14, 14, 14, 14 };

            int head = 0;
            int tail = 0;
            int capacity = _wavefrontQueue.Length;

            _wavefrontQueue[tail] = goalIndex;
            tail = (tail + 1) % capacity;

            ReadOnlySpan<byte> costs = costField.RawCosts;

            while (head != tail)
            {
                int curIndex = _wavefrontQueue[head];
                head = (head + 1) % capacity;

                int curX = curIndex % _width;
                int curY = curIndex / _width;
                ushort curCost = _integration[curIndex];

                for (int i = 0; i < 8; i++)
                {
                    int nx = curX + dx[i];
                    int ny = curY + dy[i];

                    if ((uint)nx < (uint)_width && (uint)ny < (uint)_height)
                    {
                        int nIndex = ny * _width + nx;
                        byte nCostTerrain = costs[nIndex];
                        if (nCostTerrain == CostField.CostBlocked) continue;

                        int candidate = curCost + (dCost[i] * nCostTerrain);
                        if (candidate < _integration[nIndex])
                        {
                            _integration[nIndex] = (ushort)candidate;
                            _wavefrontQueue[tail] = nIndex;
                            tail = (tail + 1) % capacity;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetValue(int x, int y) =>
            ((uint)x < (uint)_width && (uint)y < (uint)_height) ? _integration[y * _width + x] : Unvisited;
    }
}
