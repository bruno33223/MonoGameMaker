# `RectangleF` Manual da API

O `RectangleF` é uma estrutura imutável de ponto flutuante alinhada aos eixos (AABB) projetada para representar caixas delimitadoras e regiões retangulares em espaço de mundo contínuo para o subsistema RTS do MonoGameMaker. Substitui a dependência de bibliotecas externas pesadas e oferece testes de contenção e intersecção com zero alocações de Heap.

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
        public bool Contains(float x, float y);
        public bool Contains(RectangleF other);
        public bool Intersects(RectangleF other);

        public bool Equals(RectangleF other);
        public override bool Equals(object? obj);
        public override int GetHashCode();
        public static bool operator ==(RectangleF left, RectangleF right);
        public static bool operator !=(RectangleF left, RectangleF right);
        public override string ToString();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Consultas de Seleção Marquee Box:**
   Ao arrastar o cursor do mouse na tela, converta os pontos virtual/mundo inicial e final em uma instância de `RectangleF(worldStart, worldEnd)`. Passe este retângulo diretamente para `SpatialHashGrid.QueryBox(box, buffer)` para obter em sub-milissegundo todas as unidades contidas no retângulo.
2. **Testes de Colisão e Gatilhos AABB:**
   Utilize `Intersects(RectangleF other)` para verificar sobreposição entre zonas táticas, áreas de controle de objetivos ou volumes de spawn sem conversão para inteiros nem perda de precisão submétrica.
3. **Imutabilidade e Thread-Safety:**
   Como é um `readonly struct`, instâncias de `RectangleF` podem ser passadas livremente por valor ou `in` / `ref readonly` entre threads de simulação sem risco de corrupção ou contenção de memória.

---

## Exemplo Prático e Compilável

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Spatial;

public class BoundingBoxExample
{
    public void TestBounds()
    {
        var boxA = new RectangleF(100f, 100f, 200f, 150f);
        var unitPos = new Vector2(150f, 120f);

        if (boxA.Contains(unitPos))
        {
            // Unidade está dentro dos limites da caixa
        }

        var boxB = new RectangleF(new Vector2(250f, 150f), new Vector2(400f, 300f));
        if (boxA.Intersects(boxB))
        {
            // As duas regiões retangulares se sobrepõem no espaço de mundo
        }
    }
}
```
