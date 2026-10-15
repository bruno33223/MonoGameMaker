using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.Runtime.Core
{
    /// <summary>
    /// Estados formais de navegação e ciclo de vida da aplicação e metagame.
    /// Substitui o antigo enum colidente do Space_v7, segregando telas do sistema de persistência GameState.
    /// </summary>
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

    /// <summary>
    /// Controlador central de fluxo de telas e estados da aplicação.
    /// Gerencia transições atômicas, eventos de entrada e saída de estados e despacho de atualização.
    /// </summary>
    public class GameFlowController
    {
        private static GameFlowController? _instance;
        public static GameFlowController Instance => _instance ??= new GameFlowController();

        public GameFlowState CurrentState { get; private set; } = GameFlowState.BootLoading;
        public GameFlowState PreviousState { get; private set; } = GameFlowState.BootLoading;

        public float StateTime { get; private set; } = 0f;
        public Action<GameFlowState, GameFlowState>? OnStateChanged { get; set; }

        public void ChangeState(GameFlowState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;
            StateTime = 0f;

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        public void Update(float dt, InputService? input = null)
        {
            StateTime += dt;
        }
    }
}
