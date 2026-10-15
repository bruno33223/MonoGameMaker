# `IntegrationField` Manual da API

O `IntegrationField` é a segunda camada da arquitetura de navegação em massa (Flow Field) do MonoGameMaker. Ele executa uma expansão de onda de Dijkstra contígua e **Zero-GC** sobre uma matriz de inteiros não sinalizados de 16 bits (`ushort[]`), propagando as distâncias acumuladas a partir do destino (Goal = 0) para todo o mapa com pesos ortogonais (10) e diagonais (14).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pathfinding
{
    public sealed class IntegrationField
    {
        public const ushort Unvisited = ushort.MaxValue;

        public int Width { get; }
        public int Height { get; }
        public ReadOnlySpan<ushort> RawIntegration { get; }

        public IntegrationField(int width, int height);

        public void Reset();
        public void Generate(int goalX, int goalY, CostField costField);
        public ushort GetValue(int x, int y);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Geração de Campo de Potencial Global para Destino Único ou Composto:**
   Quando uma ordem de marcha ou ataque é emitida para uma coordenada, o `IntegrationField` expande a onda a partir do destino em apenas $\approx 1,8\text{ ms}$ para um mapa de $128 \times 128$. Uma vez preenchido, ele atende a centenas ou milhares de unidades instantaneamente.
2. **Eliminação de Gargalo de PriorityQueue:**
   Graças à fila circular estática pré-alocada (`_wavefrontQueue`), o algoritmo não sofre com o overhead de ponteiros ou nós de heap típicos de implementações de A\*/Dijkstra ingênuas.
3. **Detecção de Ilhas e Inacessibilidade:**
   Se uma célula possuir o valor `IntegrationField.Unvisited` (`65535`) após a execução do `Generate`, significa matematicamente que o destino é inalcançável a partir daquele ponto do mapa (bloqueado por barreiras).

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra como inicializar e gerar o `IntegrationField` a partir de uma coordenada de destino:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;

public class TargetPathfindingWavefront
{
    private readonly CostField _costField;
    private readonly IntegrationField _integrationField;

    public IntegrationField IntegrationField => _integrationField;

    public TargetPathfindingWavefront(CostField costField)
    {
        _costField = costField;
        _integrationField = new IntegrationField(costField.Width, costField.Height);
    }

    public void UpdateGoal(Vector2 worldTargetPosition)
    {
        // Converte a posição de mundo do clique do jogador para coordenadas de célula
        _costField.WorldToCell(worldTargetPosition, out int goalX, out int goalY);

        // Se o clique for em área bloqueada, não tenta gerar
        if (_costField.GetCost(goalX, goalY) == CostField.CostBlocked)
        {
            return;
        }

        // Executa o Wavefront contíguo Zero-GC
        _integrationField.Generate(goalX, goalY, _costField);
    }

    public bool CanReachGoal(Vector2 unitWorldPosition)
    {
        _costField.WorldToCell(unitWorldPosition, out int cx, out int cy);
        return _integrationField.GetValue(cx, cy) != IntegrationField.Unvisited;
    }
}
```
