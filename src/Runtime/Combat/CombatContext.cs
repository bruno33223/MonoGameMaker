using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Particles;

namespace MonoGameMaker.Runtime.Combat
{
    /// <summary>
    /// Objeto de contexto que unifica os serviços do loop de combate, eliminando o parameter tunneling.
    /// Reutilizado a cada frame (Zero-GC) e injetado nos comportamentos de entidade (EnemyEntityBehavior, Player, BossModularPart).
    /// </summary>
    public sealed class CombatContext
    {
        // --- Tempo e Escala de Simulação ---
        public float DeltaTime { get; private set; }
        public float WorldDeltaTime { get; private set; }
        public float TotalGameTime { get; private set; }
        public bool IsTimeDilated { get; set; }

        // --- Câmera e Percepção ---
        public Camera3D? Camera { get; set; }

        // --- Efeitos Visuais & Partículas ---
        public ParticleSystem3D? Particles { get; set; }

        // --- Áudio & DSP ---
        public AudioManager? Audio { get; set; }
        public SoundSynthesizer? SoundSynth { get; set; }

        // --- Entrada do Jogador ---
        public InputService? Input { get; set; }

        // --- Coordenadas do Jogador para Rastreamento de IA ---
        public Vector3 PlayerPosition { get; set; }
        public bool IsPlayerAlive { get; set; }

        // --- Gatilhos de Eventos Globais ---
        public Action? OnEmpDetonation { get; set; }
        public Action? OnCryoDetonation { get; set; }
        public Action<int>? OnScoreAdded { get; set; }

        /// <summary>
        /// Atualiza as variáveis dinâmicas de quadro antes da propagação da simulação com Zero-GC.
        /// </summary>
        public void PrepareFrame(
            float dt, 
            float worldDt, 
            float totalTime, 
            Vector3 playerPos, 
            bool playerAlive)
        {
            DeltaTime = dt;
            WorldDeltaTime = worldDt;
            TotalGameTime = totalTime;
            PlayerPosition = playerPos;
            IsPlayerAlive = playerAlive;
        }
    }
}
