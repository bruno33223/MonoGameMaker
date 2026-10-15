using System;

namespace MonoGameMaker.Runtime.Selection
{
    /// <summary>
    /// Fila estática circular de comandos para despacho sequencial de ordens RTS
    /// sem alocações de Heap no hot-loop (Zero-GC).
    /// </summary>
    public sealed class UnitCommandQueue
    {
        private readonly UnitCommand[] _buffer;
        private int _head;
        private int _tail;
        private int _count;

        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public int Capacity => _buffer.Length;

        public UnitCommandQueue(int capacity = 32)
        {
            if (capacity <= 0) capacity = 32;
            _buffer = new UnitCommand[capacity];
            Clear();
        }

        public void Enqueue(UnitCommand command, bool queueOrder = false)
        {
            if (!queueOrder)
            {
                Clear();
            }

            if (_count >= _buffer.Length)
            {
                // Descarta a ordem mais antiga para acomodar a nova sem estourar capacidade
                _head = (_head + 1) % _buffer.Length;
                _count--;
            }

            _buffer[_tail] = command;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
        }

        public bool TryPeek(out UnitCommand command)
        {
            if (_count == 0)
            {
                command = default;
                return false;
            }

            command = _buffer[_head];
            return true;
        }

        public bool TryDequeue(out UnitCommand command)
        {
            if (_count == 0)
            {
                command = default;
                return false;
            }

            command = _buffer[_head];
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return true;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
