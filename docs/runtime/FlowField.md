# `FlowField` Manual da API

O `FlowField` (e seu gestor `FlowFieldManager`) é o motor de direção vetorial contínua da navegação RTS do MonoGameMaker. Convertendo as distâncias acumuladas do `IntegrationField` em gradientes vetoriais normalizados ($-\nabla \Phi$), o `FlowField` permite que qualquer número de tropas (1 a 1.000+) navegue em tempo constante **$O(1)$** por simples amostragem de vetor, com suporte a **interpolação bilinear contínua** para erradicar quinas angulares abruptas de 45° ou 90°.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pathfinding
{
    public sealed class FlowField
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public ReadOnlySpan<Vector2> RawVectors { get; }

        public FlowField(int width, int height, float cellSize);

        public void Reset();
        public void Generate(IntegrationField integrationField, CostField costField);
        public Vector2 SampleDirection(Vector2 worldPos);
    }

    public sealed class FlowFieldManager
    {
        public FlowFieldManager(CostField costField, int poolCapacity = 16);

        public void StepFrame();
        public int RequestFlowField(Point goalCell);
        public FlowField GetField(int fieldId);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Movimentação Orgânica de Multidões em Tempo Real:**
   Cada unidade militar lê diretamente `flowField.SampleDirection(unit.Position)` no seu ciclo de física. A amostragem bilinear interpola suavemente os 4 vetores de fluxo das células circundantes com coordenadas fracionárias, produzindo trajetórias fluídas ao contornar esquinas e obstáculos.
2. **Eliminação de Tráfego Redundante:**
   Em vez de 500 chamadas de A\* calculando rotas idênticas para o mesmo destino, 1 único cálculo de `FlowField` serve a todo o exército.
3. **Gerenciamento de Múltiplos Pelotões com `FlowFieldManager`:**
   Diferentes grupos de tropas marchando para alvos distintos solicitam campos ao `FlowFieldManager`. Ele mantém um pool LRU (Least Recently Used) de campos ativos, reciclando instâncias antigas sem alocar memória no Heap.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a geração completa do campo de fluxo e a amostragem vetorial para atualizar a velocidade de marcha de unidades:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;

public class ArmyNavigationSystem
{
    private readonly CostField _costField;
    private readonly IntegrationField _integrationField;
    private readonly FlowField _flowField;

    public ArmyNavigationSystem(int cols, int rows, float cellSize)
    {
        _costField = new CostField(cols, rows, cellSize);
        _integrationField = new IntegrationField(cols, rows);
        _flowField = new FlowField(cols, rows, cellSize);
    }

    public void SetDestination(Vector2 destinationWorld)
    {
        _costField.WorldToCell(destinationWorld, out int gx, out int gy);

        // 1. Gera distâncias a partir do destino
        _integrationField.Generate(gx, gy, _costField);

        // 2. Computa o gradiente vetorial unitário (-∇Φ)
        _flowField.Generate(_integrationField, _costField);
    }

    public void MoveUnit(ref Vector2 position, ref Vector2 velocity, float maxSpeed, float deltaTime)
    {
        // 3. Amostra a direção suavizada com interpolação bilinear contínua em O(1)
        Vector2 desiredDirection = _flowField.SampleDirection(position);

        if (desiredDirection != Vector2.Zero)
        {
            Vector2 desiredVelocity = desiredDirection * maxSpeed;
            // Interpola a velocidade atual em direção à velocidade desejada (Steering simples)
            velocity = Vector2.Lerp(velocity, desiredVelocity, 10.0f * deltaTime);
        }
        else
        {
            // Chegou ao objetivo ou preso em área intransponível
            velocity = Vector2.Lerp(velocity, Vector2.Zero, 15.0f * deltaTime);
        }

        position += velocity * deltaTime;
    }
}
```
