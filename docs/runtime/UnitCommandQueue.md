# `UnitCommandQueue` Manual da API

O `UnitCommandQueue` implementa uma fila circular estática (ring buffer) de comandos táticos para unidades e esquadrões RTS no MonoGameMaker. Desenvolvido para conformidade estrita com a política Zero-GC, ele permite encadear ordens sequenciais (ex: mover até A, depois patrulhar até B) sem alocações dinâmicas no Heap do .NET.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Selection
{
    public sealed class UnitCommandQueue
    {
        public int Count { get; }
        public bool IsEmpty { get; }
        public int Capacity { get; }

        public UnitCommandQueue(int capacity = 32);

        public void Enqueue(UnitCommand command, bool queueOrder = false);
        public bool TryPeek(out UnitCommand command);
        public bool TryDequeue(out UnitCommand command);
        public void Clear();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Ordens Imediatas vs Encadeadas (Shift):**
   - Ao receber uma ordem convencional de clique direito (`queueOrder = false`), a fila atual é limpa imediatamente e a nova ordem assume a execução prioritária.
   - Ao segurar Shift (`queueOrder = true`), a nova ordem é enfileirada no final da fila sem interromper a ação em andamento.
2. **Consumo no Hot-Loop de Unidades:**
   A unidade consome ordens via `TryPeek` (para inspecionar a meta atual) e `TryDequeue` (ao concluir o objetivo atual).
3. **Zero-GC e Prevenção de Estouro:**
   Utiliza buffer pré-alocado fixo com ponteiros modulares, garantindo zero coletas de GC Gen 0 durante toda a partida.

---

## Exemplo Prático e Compilável

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Selection;

public class UnitOrderExecution
{
    private readonly UnitCommandQueue _commandQueue = new(capacity: 16);
    public Vector2 Position { get; set; }

    public void OnOrderReceived(UnitCommand command, bool isShiftHeld)
    {
        _commandQueue.Enqueue(command, queueOrder: isShiftHeld);
    }

    public void UpdateUnit()
    {
        if (_commandQueue.TryPeek(out var currentOrder))
        {
            if (Vector2.Distance(Position, currentOrder.TargetPosition) < 5f)
            {
                // Chegou ao destino, consome e avança para a próxima ordem
                _commandQueue.TryDequeue(out _);
            }
        }
    }
}
```
