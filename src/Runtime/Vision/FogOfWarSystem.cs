using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Vision
{
    /// <summary>
    /// Gerenciador de visibilidade e névoa de guerra em 3 estados com atualização desacoplada (15-20Hz)
    /// e upload dinâmico em textura Alpha8.
    /// </summary>
    public sealed class FogOfWarSystem : IDisposable
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public float CellSize { get; }

        private readonly byte[] _exploredBuffer;
        private readonly byte[] _visibleBuffer;
        private readonly byte[] _uploadBuffer;

        public Texture2D? FogTexture { get; }
        private readonly float _tickInterval;
        private float _timeAccumulator;

        public ReadOnlySpan<byte> ExploredBuffer => _exploredBuffer;
        public ReadOnlySpan<byte> VisibleBuffer => _visibleBuffer;
        public ReadOnlySpan<byte> UploadBuffer => _uploadBuffer;

        public FogOfWarSystem(GraphicsDevice? graphicsDevice, int gridWidth, int gridHeight, float cellSize, float tickRateHz = 15f)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            CellSize = cellSize;
            _tickInterval = 1.0f / tickRateHz;

            int total = gridWidth * gridHeight;
            _exploredBuffer = new byte[total];
            _visibleBuffer = new byte[total];
            _uploadBuffer = new byte[total];

            if (graphicsDevice != null)
            {
                FogTexture = new Texture2D(graphicsDevice, gridWidth, gridHeight, false, SurfaceFormat.Alpha8);
            }
            Reset();
        }

        public void Reset()
        {
            Array.Clear(_exploredBuffer, 0, _exploredBuffer.Length);
            Array.Clear(_visibleBuffer, 0, _visibleBuffer.Length);
            Array.Clear(_uploadBuffer, 0, _uploadBuffer.Length);
            FogTexture?.SetData(_uploadBuffer);
        }

        public bool ShouldTickVision(float deltaTime)
        {
            _timeAccumulator += deltaTime;
            if (_timeAccumulator >= _tickInterval)
            {
                _timeAccumulator -= _tickInterval;
                return true;
            }
            return false;
        }

        public void BeginVisionPass() => Array.Clear(_visibleBuffer, 0, _visibleBuffer.Length);

        public void RevealCircle(Vector2 worldPos, float radiusWorld)
        {
            int cx = (int)(worldPos.X / CellSize);
            int cy = (int)(worldPos.Y / CellSize);
            int r = (int)MathF.Ceiling(radiusWorld / CellSize);
            int rSq = r * r;

            int minX = Math.Max(0, cx - r);
            int maxX = Math.Min(GridWidth - 1, cx + r);
            int minY = Math.Max(0, cy - r);
            int maxY = Math.Min(GridHeight - 1, cy + r);

            for (int y = minY; y <= maxY; y++)
            {
                int dy2 = (y - cy) * (y - cy);
                int rowOffset = y * GridWidth;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy2 <= rSq)
                    {
                        _visibleBuffer[rowOffset + x] = 255;
                    }
                }
            }
        }

        public void EndVisionPass()
        {
            int total = GridWidth * GridHeight;
            for (int i = 0; i < total; i++)
            {
                byte v = _visibleBuffer[i];
                if (v == 255)
                {
                    _exploredBuffer[i] = 128;
                    _uploadBuffer[i] = 255;
                }
                else
                {
                    _uploadBuffer[i] = _exploredBuffer[i];
                }
            }

            FogTexture?.SetData(_uploadBuffer);
        }

        public FogState GetVisibilityAt(Vector2 worldPos)
        {
            int gx = (int)(worldPos.X / CellSize);
            int gy = (int)(worldPos.Y / CellSize);
            if (gx < 0 || gx >= GridWidth || gy < 0 || gy >= GridHeight)
                return FogState.Unexplored;

            int idx = gy * GridWidth + gx;
            if (_visibleBuffer[idx] == 255) return FogState.Visible;
            if (_exploredBuffer[idx] == 128) return FogState.Explored;
            return FogState.Unexplored;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityVisible(Vector2 worldPos, byte teamId, byte localPlayerTeam)
        {
            if (teamId == localPlayerTeam) return true;
            return GetVisibilityAt(worldPos) == FogState.Visible;
        }

        public void Dispose() => FogTexture?.Dispose();
    }
}
