# `FlowFieldManager` Manual da API

O `FlowFieldManager` é o gestor centralizado de campos de fluxo com cache e política de despejo LRU (Least Recently Used) do subsistema de pathfinding RTS do MonoGameMaker. Ele permite que múltiplos grupos de unidades marchem para destinos distintos de forma assíncrona, reciclando campos de fluxo existentes e recalculando dinamicamente apenas novos destinos.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pathfinding
{
    public sealed class FlowFieldManager
    {
        public int PoolCapacity { get; }

        public FlowFieldManager(CostField costField, int poolCapacity = 16);

        public void StepFrame();
        public int RequestFlowField(Point goalCell);
        public FlowField GetField(int fieldId);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Gestão de Múltiplos Alvos em Batalhas:**
   Em uma partida RTS onde o jogador ordena o Esquadrão Alpha para a base inimiga e o Esquadrão Beta para um ponto de mineração, o `FlowFieldManager` aloca slots independentes para cada destino sem custo de recomputação repetida se novas tropas receberem a mesma ordem.
2. **Desalocação Inteligente (LRU Eviction):**
   Quando a capacidade do pool é atingida (padrão 16 campos), os campos menos utilizados nos últimos quadros de simulação são substituídos sem alocações de Heap no hot-loop.
3. **Consistência de Frame:**
   Chame `StepFrame()` a cada tique de simulação para manter a estampa de tempo temporal atualizada.

---

## Exemplo Prático e Compilável

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;

public class MultiTargetNavigation
{
    private readonly FlowFieldManager _flowManager;
    private readonly CostField _costField;

    public MultiTargetNavigation(CostField costField)
    {
        _costField = costField;
        _flowManager = new FlowFieldManager(costField, poolCapacity: 8);
    }

    public void UpdateSimulation(float deltaTime)
    {
        _flowManager.StepFrame();
    }

    public void MoveUnitsTo(Point targetCell, Vector2[] unitPositions, int unitCount)
    {
        int fieldId = _flowManager.RequestFlowField(targetCell);
        FlowField field = _flowManager.GetField(fieldId);

        for (int i = 0; i < unitCount; i++)
        {
            Vector2 direction = field.SampleDirection(unitPositions[i]);
            unitPositions[i] += direction * 100f * 0.016f;
        }
    }
}
```
