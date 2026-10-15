using System;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Pathfinding
{
    /// <summary>
    /// Gerenciador de campos de fluxo com cache e política de desalocação LRU para múltiplos alvos táticos.
    /// </summary>
    public sealed class FlowFieldManager
    {
        private readonly int _poolCapacity;
        private readonly CostField _costField;
        private readonly FlowField[] _flowFields;
        private readonly IntegrationField[] _integrationFields;
        private readonly Point[] _destinations;
        private readonly uint[] _lastUsedFrame;
        private uint _currentFrame;

        public int PoolCapacity => _poolCapacity;

        public FlowFieldManager(CostField costField, int poolCapacity = 16)
        {
            ArgumentNullException.ThrowIfNull(costField);
            _costField = costField;
            _poolCapacity = poolCapacity;

            _flowFields = new FlowField[poolCapacity];
            _integrationFields = new IntegrationField[poolCapacity];
            _destinations = new Point[poolCapacity];
            _lastUsedFrame = new uint[poolCapacity];

            for (int i = 0; i < poolCapacity; i++)
            {
                _flowFields[i] = new FlowField(costField.Width, costField.Height, costField.CellSize);
                _integrationFields[i] = new IntegrationField(costField.Width, costField.Height);
                _destinations[i] = new Point(-1, -1);
            }
        }

        public void StepFrame() => _currentFrame++;

        public int RequestFlowField(Point goalCell)
        {
            // 1. Procura se o campo para esse destino já existe
            for (int i = 0; i < _poolCapacity; i++)
            {
                if (_destinations[i] == goalCell)
                {
                    _lastUsedFrame[i] = _currentFrame;
                    return i;
                }
            }

            // 2. Localiza o slot LRU (menos recentemente usado)
            int lruIndex = 0;
            uint oldestFrame = uint.MaxValue;
            for (int i = 0; i < _poolCapacity; i++)
            {
                if (_lastUsedFrame[i] < oldestFrame)
                {
                    oldestFrame = _lastUsedFrame[i];
                    lruIndex = i;
                }
            }

            // 3. Gera o novo campo no slot LRU
            _destinations[lruIndex] = goalCell;
            _lastUsedFrame[lruIndex] = _currentFrame;
            _integrationFields[lruIndex].Generate(goalCell.X, goalCell.Y, _costField);
            _flowFields[lruIndex].Generate(_integrationFields[lruIndex], _costField);

            return lruIndex;
        }

        public FlowField GetField(int fieldId)
        {
            if ((uint)fieldId < (uint)_poolCapacity)
                return _flowFields[fieldId];
            throw new ArgumentOutOfRangeException(nameof(fieldId));
        }
    }
}
