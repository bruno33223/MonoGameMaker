# `FogOfWarSystem` Manual da API

O `FogOfWarSystem` é o gerenciador de visibilidade e desinformação estratégica do módulo RTS do MonoGameMaker. Operando sobre o modelo formal de autômato de três estados (`Unexplored = 0`, `Explored = 128`, `Visible = 255`), o sistema combina projeção de linha de visão rápida circular (Line-of-Sight), atualização de textura dinâmica desacoplada a 15-20Hz e composição em shader HLSL (`FogOfWar.fx`) com curvas Hermite `smoothstep` e desaturação da penumbra.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Vision
{
    public enum FogState : byte
    {
        Unexplored = 0,    // Breu total: terreno e unidades ocultos
        Explored = 128,    // Penumbra: terreno estático revelado, unidades móveis ocultas
        Visible = 255      // Visão ativa: tudo revelado em tempo real
    }

    public sealed class FogOfWarSystem : IDisposable
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public float CellSize { get; }
        public Texture2D FogTexture { get; }

        public FogOfWarSystem(
            GraphicsDevice graphicsDevice, 
            int gridWidth, 
            int gridHeight, 
            float cellSize, 
            float tickRateHz = 15f);

        public void Reset();
        public bool ShouldTickVision(float deltaTime);

        public void BeginVisionPass();
        public void RevealCircle(Vector2 worldPos, float radiusWorld);
        public void EndVisionPass();

        public FogState GetVisibilityAt(Vector2 worldPos);

        public void Dispose();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Desacoplamento e Economia de CPU (15-20 Hz):**
   O método `ShouldTickVision(deltaTime)` garante que o Line-of-Sight seja recalculado apenas a cada 50ms a 66ms. Nos frames intermediários, o custo de CPU para visão é exatamente 0 ms, enquanto a GPU interpola suavemente a transição.
2. **Culling Tático e Ocultamento de Inimigos:**
   Antes de desenhar ou selecionar unidades inimigas, consulte `GetVisibilityAt(enemy.Position)`. Se for diferente de `FogState.Visible`, a unidade deve ser ocultada do renderizador, do minimapa e dos efeitos de áudio contextuais.
3. **Efeito Visual Orgânico em Shader:**
   A `FogTexture` (formato `SurfaceFormat.Alpha8`) é enviada à GPU e composta na cena pelo shader `FogOfWar.fx`. A amostragem bilinear suaviza os cantos das células sem deixar aspecto quadriculado.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra o ciclo de vida completo do `FogOfWarSystem` integrado ao loop de jogo:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Vision;

public class TacticalFogController
{
    private readonly FogOfWarSystem _fogSystem;
    private readonly Vector2[] _friendlyUnits;
    private readonly float _visionRadius = 120.0f;

    public Texture2D FogTexture => _fogSystem.FogTexture;

    public TacticalFogController(GraphicsDevice graphicsDevice, int worldWidth, int worldHeight)
    {
        // Grade de 128x128 com células de 16 pixels cobrindo mapa de 2048x2048
        _fogSystem = new FogOfWarSystem(graphicsDevice, 128, 128, 16.0f, tickRateHz: 15.0f);

        _friendlyUnits = new Vector2[]
        {
            new Vector2(250, 250),
            new Vector2(300, 280),
            new Vector2(500, 450)
        };
    }

    public void Update(float deltaTime)
    {
        // 1. Verifica se é momento de rodar o tick de visão (15 Hz)
        if (_fogSystem.ShouldTickVision(deltaTime))
        {
            _fogSystem.BeginVisionPass();

            // 2. Projeta o raio de visão de cada tropa aliada
            for (int i = 0; i < _friendlyUnits.Length; i++)
            {
                _fogSystem.RevealCircle(_friendlyUnits[i], _visionRadius);
            }

            // 3. Mescla buffers e transfere os 16 KB para a Texture2D via SetData
            _fogSystem.EndVisionPass();
        }
    }

    public bool IsEnemyVisible(Vector2 enemyPosition)
    {
        // 4. Determina se a unidade inimiga deve ser renderizada e clicável
        return _fogSystem.GetVisibilityAt(enemyPosition) == FogState.Visible;
    }
}
```
