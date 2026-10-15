# `InstancedUnitRenderer` Manual da API

O `InstancedUnitRenderer` é o módulo de renderização em massa de alta performance do MonoGameMaker. Aproveitando a tecnologia de **GPU Hardware Instancing** (`DrawInstancedPrimitives`), ele desacopla a geometria (Stream 0: Quad estático de 4 vértices do tipo canônico `Microsoft.Xna.Framework.Graphics.VertexPositionTexture`) dos atributos da entidade (Stream 1: `UnitInstanceData` de 64 bytes), permitindo desenhar **500 a 1.000 unidades militares com animação de atlas UV e substituição de cor de facção em uma única Draw Call** no MonoGame DesktopGL (OpenGL 3.3 Core).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Graphics3D
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UnitInstanceData : IVertexType
    {
        public Vector4 TransformData; // X, Y = WorldPos, Z = Rotation, W = Scale (16 bytes)
        public Vector4 AtlasUVRect;   // X = UMin, Y = VMin, Z = UWidth, W = VHeight (16 bytes)
        public Vector4 TeamColor;     // RGBA Cor da Facção (16 bytes)
        public Vector4 VisualFlags;   // X = IsSelected, Y = HealthRatio, Z/W = Reservados (16 bytes) -> Total 64 bytes

        public static readonly VertexDeclaration VertexDeclaration;
    }

    public sealed class InstancedUnitRenderer : IDisposable
    {
        public InstancedUnitRenderer(GraphicsDevice graphicsDevice);

        public void BeginBatch();

        public void Submit(
            Vector2 worldPos, 
            float rotation, 
            float scale, 
            Vector4 atlasUV, 
            Color teamColor, 
            bool isSelected, 
            float healthRatio);

        public void Render(Effect instancingEffect, Matrix viewProjection);

        public void Dispose();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Substituição do `SpriteBatch` em Batalhas Massivas:**
   Desenhar 1.000 sprites individuais via `SpriteBatch.Draw()` causa gargalo de CPU na montagem de vértices e na fila de batches. O `InstancedUnitRenderer` transfere a rotação e expansão de vértices para a GPU, reduzindo o custo de CPU a quase zero.
2. **Personalização Dinâmica de Cores de Jogadores (*Team Color*):**
   O canal `TeamColor` da struct permite que diferentes exércitos compartilhem a mesma textura base de atlas de sprites, tendo as partes marcadas com máscara tingidas diretamente no pixel shader da GPU.
3. **Indicadores Visuais Integrados:**
   Através de `VisualFlags`, o shader desenha contornos de seleção e taxas de pontos de vida sem requerer passes extras de desenho.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra o preenchimento do lote e a chamada de renderização com o shader de instanciamento:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Graphics3D;

public class ArmyRenderPass
{
    private readonly InstancedUnitRenderer _renderer;
    private readonly Effect _instancingEffect;

    public ArmyRenderPass(GraphicsDevice graphicsDevice, Effect instancingEffect)
    {
        _renderer = new InstancedUnitRenderer(graphicsDevice);
        _instancingEffect = instancingEffect;
    }

    public void DrawArmy(Matrix viewProjectionMatrix, Vector2[] positions, float[] rotations, Color armyColor)
    {
        // 1. Inicia o lote de desenho do quadro atual
        _renderer.BeginBatch();

        Vector4 idleUV = new Vector4(0.0f, 0.0f, 0.25f, 0.25f); // 1º quadro do atlas 4x4

        // 2. Submete até 1.000 unidades em memória contígua (Zero-GC)
        for (int i = 0; i < positions.Length; i++)
        {
            _renderer.Submit(
                worldPos: positions[i],
                rotation: rotations[i],
                scale: 32.0f,
                atlasUV: idleUV,
                teamColor: armyColor,
                isSelected: (i == 0), // Primeira unidade selecionada para teste
                healthRatio: 1.0f
            );
        }

        // 3. Dispara a Draw Call única para a GPU
        _renderer.Render(_instancingEffect, viewProjectionMatrix);
    }

    public void Teardown()
    {
        _renderer.Dispose();
    }
}
```
