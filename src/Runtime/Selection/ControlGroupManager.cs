using System;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Selection
{
    /// <summary>
    /// Gerenciador de grupos táticos (Ctrl+1..9 e Shift+1..9) com descarte de unidades mortas
    /// e detecção de duplo clique para salto de câmera.
    /// </summary>
    public sealed class ControlGroupManager
    {
        private readonly int[][] _groups = new int[10][];
        private readonly int[] _groupCounts = new int[10];
        private readonly float[] _lastSelectTime = new float[10];
        private const float DoubleTapThreshold = 0.25f;

        public ControlGroupManager(int maxUnitsPerGroup = 256)
        {
            for (int i = 0; i < 10; i++)
                _groups[i] = new int[maxUnitsPerGroup];
        }

        /// <summary>
        /// Associa a seleção ao grupo tático indicado (Ctrl + 1..9).
        /// </summary>
        public void AssignGroup(int slot, ReadOnlySpan<int> entities)
        {
            if ((uint)slot >= 10) return;
            int count = Math.Min(entities.Length, _groups[slot].Length);
            entities.Slice(0, count).CopyTo(_groups[slot]);
            _groupCounts[slot] = count;
        }

        /// <summary>
        /// Adiciona unidades ao grupo tático sem duplicatas (Shift + 1..9).
        /// </summary>
        public void AddToGroup(int slot, ReadOnlySpan<int> entities)
        {
            if ((uint)slot >= 10) return;
            int currentCount = _groupCounts[slot];
            Span<int> groupSpan = _groups[slot].AsSpan(0, currentCount);

            for (int i = 0; i < entities.Length && _groupCounts[slot] < _groups[slot].Length; i++)
            {
                int id = entities[i];
                if (!groupSpan.Contains(id))
                {
                    _groups[slot][_groupCounts[slot]++] = id;
                    groupSpan = _groups[slot].AsSpan(0, _groupCounts[slot]);
                }
            }
        }

        /// <summary>
        /// Seleciona o grupo tático no slot indicado. Detecta duplo clique em intervalo inferior a 0.25s.
        /// </summary>
        public bool SelectGroup(int slot, float currentTime, out ReadOnlySpan<int> entities, out bool doubleTapJump)
        {
            return SelectGroup(slot, currentTime, null, out entities, out doubleTapJump);
        }

        /// <summary>
        /// Seleciona o grupo tático descartando unidades mortas/destruídas através do predicado de sobrevivência.
        /// </summary>
        public bool SelectGroup(int slot, float currentTime, Func<int, bool>? isAlive, out ReadOnlySpan<int> entities, out bool doubleTapJump)
        {
            doubleTapJump = false;
            entities = ReadOnlySpan<int>.Empty;
            if ((uint)slot >= 10 || _groupCounts[slot] == 0) return false;

            if (isAlive != null)
            {
                PruneDeadInSlot(slot, isAlive);
                if (_groupCounts[slot] == 0) return false;
            }

            if (currentTime - _lastSelectTime[slot] <= DoubleTapThreshold)
                doubleTapJump = true;

            _lastSelectTime[slot] = currentTime;
            entities = _groups[slot].AsSpan(0, _groupCounts[slot]);
            return true;
        }

        /// <summary>
        /// Seleciona o grupo tático descartando unidades destruídas e calcula o centróide das posições.
        /// </summary>
        public bool SelectGroup(
            int slot,
            float currentTime,
            Func<int, bool>? isAlive,
            out ReadOnlySpan<int> entities,
            out bool doubleTapJump,
            ReadOnlySpan<Vector2> positions,
            out Vector2 centroid)
        {
            centroid = Vector2.Zero;
            bool result = SelectGroup(slot, currentTime, isAlive, out entities, out doubleTapJump);
            if (result && !positions.IsEmpty)
            {
                centroid = CalculateCentroid(entities, positions);
            }
            return result;
        }

        /// <summary>
        /// Remove entidades destruídas de um slot específico.
        /// </summary>
        public void PruneDeadInSlot(int slot, Func<int, bool> isAlive)
        {
            if ((uint)slot >= 10) return;
            ArgumentNullException.ThrowIfNull(isAlive);

            int count = _groupCounts[slot];
            int validCount = 0;
            for (int i = 0; i < count; i++)
            {
                int id = _groups[slot][i];
                if (isAlive(id))
                {
                    _groups[slot][validCount++] = id;
                }
            }
            _groupCounts[slot] = validCount;
        }

        /// <summary>
        /// Remove entidades destruídas de todos os grupos táticos.
        /// </summary>
        public void PruneDead(Func<int, bool> isAlive)
        {
            for (int i = 0; i < 10; i++)
            {
                PruneDeadInSlot(i, isAlive);
            }
        }

        /// <summary>
        /// Calcula o centro médio (centróide) do grupo selecionado para focalização de câmera.
        /// </summary>
        public static Vector2 CalculateCentroid(ReadOnlySpan<int> entities, ReadOnlySpan<Vector2> positions)
        {
            if (entities.Length == 0) return Vector2.Zero;
            Vector2 sum = Vector2.Zero;
            for (int i = 0; i < entities.Length; i++)
            {
                sum += positions[entities[i]];
            }
            return sum / entities.Length;
        }
    }
}
