using Microsoft.Xna.Framework;

namespace MonoGameMaker.Runtime.Selection
{
    /// <summary>
    /// Papel tático da unidade no campo de batalha para ordenação e priorização de seleção.
    /// </summary>
    public enum UnitTacticalRole : byte
    {
        Combat = 0,    // Soldado, Tanque, Aeronave de combate
        Worker = 1     // Colhedor, Construtor, Civil
    }

    /// <summary>
    /// Tipo de ordem/comando emitida para unidades RTS.
    /// </summary>
    public enum UnitCommandType : byte
    {
        Move,
        AttackMove,
        HoldPosition,
        Stop,
        AttackTarget
    }

    /// <summary>
    /// Comando tático imutável despachado para unidades individuais ou grupos selecionados.
    /// </summary>
    public readonly struct UnitCommand
    {
        public UnitCommandType Type { get; }
        public Vector2 TargetPosition { get; }
        public int TargetEntityId { get; }

        public UnitCommand(UnitCommandType type, Vector2 targetPos, int targetEntityId = -1)
        {
            Type = type;
            TargetPosition = targetPos;
            TargetEntityId = targetEntityId;
        }
    }
}
