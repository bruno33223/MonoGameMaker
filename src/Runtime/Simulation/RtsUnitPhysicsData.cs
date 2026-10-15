using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Simulation
{
    /// <summary>
    /// Estrutura de dados contígua compacta de 48 bytes (alinhada em linha de cache) para simulação de física RTS.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RtsUnitPhysicsData
    {
        public Vector2 Position;       // 8 bytes
        public Vector2 Velocity;       // 8 bytes
        public float Rotation;         // 4 bytes
        public float Radius;           // 4 bytes
        public float MaxSpeed;         // 4 bytes
        public float MaxForce;         // 4 bytes
        public float Mass;             // 4 bytes
        public float TurnSpeed;        // 4 bytes
        public int FlowFieldId;        // 4 bytes
        public byte IsActive;          // 1 byte
        public byte TeamId;            // 1 byte
        private ushort _padding;       // 2 bytes de alinhamento
    }
}
