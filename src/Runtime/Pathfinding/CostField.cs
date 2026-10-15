using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Pathfinding
{
    /// <summary>
    /// Matriz contígua de custos de terreno e obstáculos intransponíveis para cálculo de campos de fluxo.
    /// </summary>
    public sealed class CostField
    {
        public const byte CostPassable = 1;
        public const byte CostBlocked = 255;

        private readonly int _width;
        private readonly int _height;
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly byte[] _costs;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public ReadOnlySpan<byte> RawCosts => _costs;

        public CostField(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _invCellSize = 1.0f / cellSize;
            _costs = new byte[width * height];
            Array.Fill(_costs, CostPassable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCost(int x, int y, byte cost)
        {
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                _costs[y * _width + x] = cost;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetCost(int x, int y)
        {
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                return _costs[y * _width + x];
            return CostBlocked;
        }

        public void WorldToCell(Vector2 worldPos, out int cellX, out int cellY)
        {
            cellX = (int)(worldPos.X * _invCellSize);
            cellY = (int)(worldPos.Y * _invCellSize);
        }

        public Vector2 CellToWorld(int cellX, int cellY) =>
            new Vector2((cellX + 0.5f) * _cellSize, (cellY + 0.5f) * _cellSize);

        public void Clear(byte defaultCost = CostPassable) => Array.Fill(_costs, defaultCost);
    }
}
