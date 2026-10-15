# `UnitInstanceData` Manual da API

O `UnitInstanceData` é a estrutura de dados de vértice por instância (Stream 1) com alinhamento rigoroso de 64 bytes (`Pack = 1`, correspondendo exatamente a 4 registradores `vec4` e a 1 linha de cache L1 da CPU) para renderização em lote via GPU Hardware Instancing de unidades militares RTS no MonoGameMaker.

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
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **GPU Hardware Instancing de Alto Rendimento:**
   Alimenta o buffer secundário (`DynamicVertexBuffer`) no Stream 1 de `InstancedUnitRenderer`, permitindo que 1.000 unidades militares sejam desenhadas com 1 única chamada `DrawInstancedPrimitives`.
2. **Substituição de Cor de Facção (Team Color):**
   O campo `TeamColor` passa a cor normalizada do jogador para o shader HLSL (`UnitInstanced.fx`), que pinta a camuflagem da unidade conforme a máscara de textura.
3. **Animação UV por Sprite Atlas:**
   O campo `AtlasUVRect` permite que cada unidade renderize um frame de animação diferente ou orientação direcional distinta a partir do mesmo Texture Atlas sem trocar de textura.

---

## Exemplo Prático e Compilável

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Graphics3D;

public class UnitBatchSetup
{
    public UnitInstanceData CreateInstanceData(
        Vector2 position, 
        float rotation, 
        float scale, 
        Vector4 uvBounds, 
        Color factionColor, 
        bool isSelected, 
        float healthPercent)
    {
        return new UnitInstanceData
        {
            TransformData = new Vector4(position.X, position.Y, rotation, scale),
            AtlasUVRect = uvBounds,
            TeamColor = factionColor.ToVector4(),
            VisualFlags = new Vector4(isSelected ? 1f : 0f, healthPercent, 0f, 0f)
        };
    }
}
```
