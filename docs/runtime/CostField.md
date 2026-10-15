# `CostField` Manual da API

O `CostField` é a camada base de topografia e transitabilidade do sistema de navegação por campos de fluxo (Flow Field) do MonoGameMaker. Armazenado como uma matriz 1D contígua de bytes (`byte[]`), ele encapsula as dificuldades de locomoção do terreno e obstáculos intransponíveis (rochas, muralhas, água profunda) ocupando ínfimos $\approx 16\text{ KB}$ de memória para grades de $128 \times 128$ células.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Pathfinding
{
    public sealed class CostField
    {
        public const byte CostPassable = 1;
        public const byte CostBlocked = 255;

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public ReadOnlySpan<byte> RawCosts { get; }

        public CostField(int width, int height, float cellSize);

        public void SetCost(int x, int y, byte cost);
        public byte GetCost(int x, int y);

        public void WorldToCell(Vector2 worldPos, out int cellX, out int cellY);
        public Vector2 CellToWorld(int cellX, int cellY);

        public void Clear(byte defaultCost = CostPassable);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Definição de Obstáculos Estáticos e Dinâmicos:**
   Ao carregar o mapa ou construir uma estrutura no jogo, defina as células ocupadas com `CostField.CostBlocked` (valor 255). Qualquer algoritmo subsequente de Wavefront ignorará automaticamente estas células.
2. **Penalização de Terrenos Friccionais:**
   Atribua custos intermediários para lamaçal (ex: 5), água rasa (ex: 8) ou estradas pavimentadas (custo nominal 1). Tropas evitarão pântanos a menos que contorná-los represente uma distância excessiva.
3. **Conversão Bidirecional Mundo $\leftrightarrow$ Grade:**
   Utilize `WorldToCell` para descobrir a célula em que um clique do mouse ou uma unidade se encontra, e `CellToWorld` para obter a coordenada central da célula em pixels/unidades de mundo.
4. **Alimentação Direta do `IntegrationField`:**
   O `CostField` é a entrada matemática mandatória para a propagação de ondas do `IntegrationField`.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra como instanciar o `CostField`, carregar barreiras intransponíveis a partir de colisores estáticos do cenário e consultar custos:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;

public class TerrainNavigationSetup
{
    private readonly CostField _costField;

    public CostField CostField => _costField;

    public TerrainNavigationSetup(int worldWidth, int worldHeight, float cellSize)
    {
        int cols = (int)(worldWidth / cellSize);
        int rows = (int)(worldHeight / cellSize);

        _costField = new CostField(cols, rows, cellSize);

        // Configura uma área de pântano (alta fricção)
        ApplySwampArea(new Rectangle(20, 20, 15, 10), cost: 6);

        // Constrói uma muralha intransponível
        ApplyObstacleWall(startX: 40, startY: 10, length: 30, isHorizontal: false);
    }

    public void ApplySwampArea(Rectangle cellBounds, byte cost)
    {
        for (int y = cellBounds.Top; y < cellBounds.Bottom; y++)
        {
            for (int x = cellBounds.Left; x < cellBounds.Right; x++)
            {
                _costField.SetCost(x, y, cost);
            }
        }
    }

    public void ApplyObstacleWall(int startX, int startY, int length, bool isHorizontal)
    {
        for (int i = 0; i < length; i++)
        {
            int cx = isHorizontal ? startX + i : startX;
            int cy = isHorizontal ? startY : startY + i;
            _costField.SetCost(cx, cy, CostField.CostBlocked);
        }
    }

    public bool IsPositionWalkable(Vector2 worldPosition)
    {
        _costField.WorldToCell(worldPosition, out int cx, out int cy);
        return _costField.GetCost(cx, cy) != CostField.CostBlocked;
    }
}
```
