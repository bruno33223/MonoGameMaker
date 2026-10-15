# `GameFlowController` Manual da API

O `GameFlowController` é a máquina de estados global que gerencia o ciclo de vida e a transição entre telas do jogo (Boot, Título, Seleção de Perfil, Hub Central, Combate Ativo, Intermissão com Mercador, Vitória e Derrota). Ele substitui de forma desacoplada o antigo chaveamento monolítico de `Game1`, segregando o fluxo da aplicação da autoridade de persistência `GameState`.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Core
{
    public enum GameFlowState
    {
        BootLoading,
        TitleScreen,
        ProfileSelect,
        MainHub,
        SystemMap,
        SectorBriefing,
        PlayingCombat,
        CombatPaused,
        MerchantIntermission,
        UpgradeDraft,
        RamDraft,
        GameOver,
        Victory,
        TutorialGlitchPrompt,
        DevSandbox
    }

    public class GameFlowController
    {
        public static GameFlowController Instance { get; }

        public GameFlowState CurrentState { get; }
        public GameFlowState PreviousState { get; }
        public float StateTime { get; }

        public Action<GameFlowState, GameFlowState>? OnStateChanged { get; set; }

        public void ChangeState(GameFlowState newState);
        public void Update(float dt, InputService? input = null);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Eliminação de Modais Sobrepostos no Loop Principal:** O `GameFlowController` centraliza o estado global de execução. Em vez de condicionais booleanas dispersas (`if (isPaused) ... if (inMerchant) ...`), o estado corrente governa quais controladores de cena recebem input e renderização.
2. **Resolução de Conflito com `GameState`:** O antigo enum `GameState` colidia com a persistência de metagame da engine. O uso explícito de `GameFlowState` mantém intactas as responsabilidades de cada classe.
3. **Temporizadores de Transição:** A propriedade `StateTime` rastreia automaticamente quanto tempo a aplicação permanece no estado ativo, viabilizando animações de fade e bloqueios temporários de input durante telas de transição.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a escuta de transições de estado e o fluxo entre telas no ciclo da aplicação:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Core;

namespace MyGame.Flow
{
    public class GameManager
    {
        public void Initialize()
        {
            // Registra listener para carregar ou pausar música em transições
            GameFlowController.Instance.OnStateChanged = (previous, current) =>
            {
                if (current == GameFlowState.PlayingCombat)
                {
                    // Inicia loop de combate
                }
                else if (current == GameFlowState.GameOver)
                {
                    // Toca fanfarra de derrota
                }
            };

            // Inicia fluxo na tela de título
            GameFlowController.Instance.ChangeState(GameFlowState.TitleScreen);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = InputService.Instance;
            input.Update(gameTime);

            // Atualiza temporizador de estado
            GameFlowController.Instance.Update(dt, input);

            // Transição via input
            if (GameFlowController.Instance.CurrentState == GameFlowState.TitleScreen && input.IsFireJustPressed())
            {
                GameFlowController.Instance.ChangeState(GameFlowState.MainHub);
            }
        }
    }
}
```
