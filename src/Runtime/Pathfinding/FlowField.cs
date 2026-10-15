using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Pathfinding
{
    /// <summary>
    /// Campo de fluxo vetorial contínuo derivado da integração de custos,
    /// com amostragem bilinear em O(1) sem saltos angulares.
    /// </summary>
    public sealed class FlowField
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Vector2[] _vectors;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public ReadOnlySpan<Vector2> RawVectors => _vectors;

        public FlowField(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _invCellSize = 1.0f / cellSize;
            _vectors = new Vector2[width * height];
            Reset();
        }

        public void Reset() => Array.Fill(_vectors, Vector2.Zero);

        public void Generate(IntegrationField integrationField, CostField costField)
        {
            Reset();

            ReadOnlySpan<int> dx = stackalloc int[8] { 0, 0, -1, 1, -1, 1, -1, 1 };
            ReadOnlySpan<int> dy = stackalloc int[8] { -1, 1, 0, 0, -1, -1, 1, 1 };

            ReadOnlySpan<byte> costs = costField.RawCosts;
            ReadOnlySpan<ushort> integ = integrationField.RawIntegration;

            for (int y = 0; y < _height; y++)
            {
                int rowOffset = y * _width;
                for (int x = 0; x < _width; x++)
                {
                    int index = rowOffset + x;
                    if (costs[index] == CostField.CostBlocked || integ[index] == 0)
                    {
                        _vectors[index] = Vector2.Zero;
                        continue;
                    }

                    int bestCost = integ[index];
                    Vector2 bestDir = Vector2.Zero;

                    for (int i = 0; i < 8; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];

                        if ((uint)nx < (uint)_width && (uint)ny < (uint)_height)
                        {
                            int nIndex = ny * _width + nx;
                            ushort nVal = integ[nIndex];
                            if (nVal < bestCost)
                            {
                                bestCost = nVal;
                                bestDir = new Vector2(dx[i], dy[i]);
                            }
                        }
                    }

                    if (bestDir != Vector2.Zero)
                    {
                        bestDir.Normalize();
                        _vectors[index] = bestDir;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 SampleDirection(Vector2 worldPos)
        {
            float fx = worldPos.X * _invCellSize;
            float fy = worldPos.Y * _invCellSize;
            int cx = (int)MathF.Floor(fx);
            int cy = (int)MathF.Floor(fy);

            if (cx < 0 || cx >= _width - 1 || cy < 0 || cy >= _height - 1)
            {
                int clampX = Math.Clamp(cx, 0, _width - 1);
                int clampY = Math.Clamp(cy, 0, _height - 1);
                return _vectors[clampY * _width + clampX];
            }

            float u = fx - cx;
            float v = fy - cy;

            Vector2 v00 = _vectors[cy * _width + cx];
            Vector2 v10 = _vectors[cy * _width + (cx + 1)];
            Vector2 v01 = _vectors[(cy + 1) * _width + cx];
            Vector2 v11 = _vectors[(cy + 1) * _width + (cx + 1)];

            Vector2 interpolated = (1.0f - u) * (1.0f - v) * v00
                                 + u * (1.0f - v) * v10
                                 + (1.0f - u) * v * v01
                                 + u * v * v11;

            if (interpolated.LengthSquared() > 0.001f)
            {
                interpolated.Normalize();
                return interpolated;
            }

            return v00;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 SampleSmoothedDirection(Vector2 worldPos) => SampleDirection(worldPos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetVector(int cellX, int cellY)
        {
            if ((uint)cellX < (uint)_width && (uint)cellY < (uint)_height)
                return _vectors[cellY * _width + cellX];
            return Vector2.Zero;
        }
    }
}
