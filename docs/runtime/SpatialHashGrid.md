# `SpatialHashGrid` Manual da API

O `SpatialHashGrid` é o acelerador espacial de alta densidade e **Zero-GC** do MonoGameMaker, concebido especificamente para simulação e consulta em massa de 500 a 1.000 unidades militares simultâneas. Implementado sobre a técnica de **Static Linked Arrays** (análoga a grafos em Compressed Sparse Row - CSR), ele substitui o uso de dicionários e listas dinâmicas no Heap por arrays contíguos de inteiros (`cellHead`, `nextEntity`, `entityCell`), garantindo que toda a estrutura espacial resida no Cache L1 da CPU (consumindo apenas $\approx 24\text{ KB}$ de memória).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Spatial
{
    public readonly struct RectangleF : IEquatable<RectangleF>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;
        public Vector2 Min => new Vector2(X, Y);
        public Vector2 Max => new Vector2(Right, Bottom);

        public RectangleF(float x, float y, float width, float height);
        public RectangleF(Vector2 min, Vector2 max);
        public bool Contains(Vector2 point);
        public bool Equals(RectangleF other);
    }

    public sealed class SpatialHashGrid
    {
        public int CellColumns { get; }
        public int CellRows { get; }
        public float CellSize { get; }
        public int MaxEntities { get; }
        public ReadOnlySpan<Vector2> Positions { get; }

        public SpatialHashGrid(int worldWidth, int worldHeight, float cellSize, int maxEntities = 2048);

        public void Clear();
        public void Insert(int entityId, Vector2 position);
        public void Remove(int entityId);
        public void UpdatePosition(int entityId, Vector2 oldPos, Vector2 newPos);

        public int FindNeighbors(
            Vector2 center, 
            float radius, 
            Span<int> results, 
            int ignoreEntityId = -1);

        public int QueryBox(
            RectangleF box, 
            Span<int> results);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Repulsão e Separação de Enxames (Reynolds Steering):**
   No hot loop de física de 1.000 soldados, invoque `FindNeighbors(pos, radius, spanBuffer, i)`. O método varre apenas o bloco $3 \times 3$ de células vizinhas imediatas e preenche o buffer fornecido sem instanciar qualquer objeto no Heap (.NET GC Generation 0 Zero).
2. **Seleção Tática por Caixa de Arrasto (Marquee Box):**
   Ao soltar o botão do mouse, a caixa delimitadora do mundo é passada para `QueryBox(box, selectionBuffer)`. A grade descarta instantaneamente 99% do mapa e retorna em sub-milissegundo os índices das unidades contidas na seleção.
3. **Detecção e Resolução de Penetração Rígida (Circle-Circle Push):**
   Use `FindNeighbors` com o raio de contato físico da unidade ($2 \times R$) para aplicar o relaxamento posicional de colisão de corpos rígidos sem complexidade quadrática $O(N^2)$.
4. **Atualização Dinâmica vs Reindexação Global:**
   - Se a maioria das unidades se move a cada frame: chame `Clear()` seguido de `Insert(id, pos)` em loop $O(N)$ linear.
   - Se poucas unidades se movem: chame `UpdatePosition(id, oldPos, newPos)`, que preserva os ponteiros se a unidade permaneceu na mesma célula.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra a utilização do `SpatialHashGrid` em um ciclo clássico de atualização e resolução de vizinhos:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Spatial;

public class SpatialArmyController
{
    private readonly SpatialHashGrid _grid;
    private readonly Vector2[] _positions;
    private readonly int[] _neighborsBuffer;
    private readonly int _unitCount = 500;

    public SpatialArmyController()
    {
        // Mapa de 2048x2048 com células de 32 pixels e capacidade para até 1024 unidades
        _grid = new SpatialHashGrid(2048, 2048, 32.0f, 1024);
        _positions = new Vector2[_unitCount];
        _neighborsBuffer = new int[64]; // Suporta até 64 vizinhos por unidade

        // Inicializa tropas em posições de teste
        var rnd = new Random(42);
        for (int i = 0; i < _unitCount; i++)
        {
            _positions[i] = new Vector2(rnd.Next(200, 800), rnd.Next(200, 800));
        }
    }

    public void Update(float deltaTime)
    {
        // 1. Limpa a grade no início da simulação física (O(C) com memset)
        _grid.Clear();

        // 2. Insere todas as tropas ativas na grade em O(N)
        for (int i = 0; i < _unitCount; i++)
        {
            _grid.Insert(i, _positions[i]);
        }

        Span<int> neighborSpan = _neighborsBuffer.AsSpan();

        // 3. Consulta de vizinhança para cada unidade (Zero-GC absoluto)
        for (int i = 0; i < _unitCount; i++)
        {
            Vector2 currentPos = _positions[i];
            float queryRadius = 24.0f;

            int count = _grid.FindNeighbors(currentPos, queryRadius, neighborSpan, ignoreEntityId: i);

            Vector2 separationForce = Vector2.Zero;
            for (int n = 0; n < count; n++)
            {
                int neighborId = neighborSpan[n];
                Vector2 toNeighbor = currentPos - _positions[neighborId];
                float distSq = toNeighbor.LengthSquared();

                if (distSq > 0.001f)
                {
                    separationForce += toNeighbor / (distSq + 0.1f);
                }
            }

            // Aplica separação suave à posição da tropa
            _positions[i] += separationForce * (30.0f * deltaTime);
        }
    }

    public void SelectUnitsInArea(Vector2 dragStart, Vector2 dragEnd, Span<int> selectedOut, out int totalSelected)
    {
        var box = new RectangleF(dragStart, dragEnd);
        totalSelected = _grid.QueryBox(box, selectedOut);
    }
}
```
