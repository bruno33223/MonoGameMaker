# `FogState` Manual da API

O `FogState` define os três estados discretos do autômato celular formal que governa a visibilidade e desinformação estratégica no sistema de Névoa de Guerra do MonoGameMaker.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Vision
{
    public enum FogState : byte
    {
        Unexplored = 0,    // Breu total
        Explored = 128,    // Penumbra (apenas mapa estático visível)
        Visible = 255      // Linha de visão ativa (unidades inimigas visíveis)
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Classificação de Visibilidade de Terreno e Tropas:**
   - `Unexplored (0)`: Terreno completamente desconhecido. Oculta mapa, edifícios e tropas.
   - `Explored (128)`: Região visitada anteriormente. Revela relevo e construções estáticas congeladas (*Fog Ghost*), mas oculta unidades móveis inimigas.
   - `Visible (255)`: Região sob observação ativa por pelo menos uma unidade aliada. Revela tudo em tempo real.
2. **Culling de Renderização e Lógica de Combate:**
   Unidades inimigas em células com `FogState != Visible` não devem ser desenhadas no frame nem podem ser alvejadas automaticamente por tropas aliadas fora de alcance.

---

## Exemplo Prático e Compilável

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Vision;

public class TacticalVisibilityChecker
{
    public void CheckUnit(Vector2 enemyPos, FogOfWarSystem fogSystem)
    {
        FogState state = fogSystem.GetVisibilityAt(enemyPos);
        if (state == FogState.Visible)
        {
            // Inimigo avistado: desenhar no mapa e habilitar engajamento
        }
        else
        {
            // Inimigo oculto pela névoa: aplicar culling
        }
    }
}
```
