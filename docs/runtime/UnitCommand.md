# `UnitCommand` Manual da API

O `UnitCommand` encapsula os tipos de ordens táticas e dados de parâmetros imutáveis emitidos pelo jogador ou rotinas de inteligência artificial para unidades individuais ou batalhões em marcha no módulo RTS do MonoGameMaker.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Selection
{
    public enum UnitTacticalRole : byte
    {
        Combat = 0,    // Soldado, Tanque, Aeronave
        Worker = 1     // Colhedor, Construtor, Civil
    }

    public enum UnitCommandType : byte
    {
        Move,
        AttackMove,
        HoldPosition,
        Stop,
        AttackTarget
    }

    public readonly struct UnitCommand
    {
        public UnitCommandType Type { get; }
        public Vector2 TargetPosition { get; }
        public int TargetEntityId { get; }

        public UnitCommand(UnitCommandType type, Vector2 targetPos, int targetEntityId = -1);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Despacho Desacoplado de Ordens:**
   Utilizado pelo `RTSSelectionManager.IssueCommand(command, dispatcher)` para enviar ordens sem acoplamento direto com a máquina de estados interna de cada unidade.
2. **Encadeamento de Ações:**
   Armazenável em filas de comandos (`Queue<UnitCommand>`) para suporte a ações sequenciais encadeadas (ex: segurar Shift para mover até ponto A e depois patrulhar até ponto B).
3. **Filtragem de Papéis Táticos (`UnitTacticalRole`):**
   Permite priorizar unidades de combate em relação a trabalhadores ao arrastar a caixa de seleção elástica.

---

## Exemplo Prático e Compilável

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Selection;

public class UnitOrderQueue
{
    private readonly Queue<UnitCommand> _commandQueue = new();

    public void GiveCommand(UnitCommand command, bool shiftHeld)
    {
        if (!shiftHeld)
        {
            _commandQueue.Clear();
        }
        _commandQueue.Enqueue(command);
    }

    public void UpdateUnit()
    {
        if (_commandQueue.TryPeek(out var currentOrder))
        {
            switch (currentOrder.Type)
            {
                case UnitCommandType.Move:
                    // Navegar até currentOrder.TargetPosition
                    break;
                case UnitCommandType.AttackMove:
                    // Mover atacando inimigos no caminho
                    break;
                case UnitCommandType.HoldPosition:
                    // Manter posição fixa sem perseguir alvos
                    break;
                case UnitCommandType.Stop:
                    _commandQueue.Dequeue();
                    break;
            }
        }
    }
}
```
