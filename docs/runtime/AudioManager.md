# `AudioManager` Manual da API

O `AudioManager` é a fachada de controle acústico centralizada do MonoGameMaker. Ele implementa barramentos independentes de volume (Master, SFX, BGM), limitação estrita de vozes concorrentes por categoria (*Voice Stealing*), supressão de saturação acústica (*Throttling / Debounce*), watchdog para purga de loops órfãos e o inovador Sistema de Acúmulo de Detonações (*Explosion Accretion System*).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Audio
{
    public class AudioManager
    {
        public static AudioManager Instance { get; }

        public float MasterVolume { get; set; }
        public float SfxVolume { get; set; }
        public float MusicVolume { get; set; }

        public const double AccretionWindow = 0.120;
        public const double LoopHeartbeatTimeout = 0.150;

        public void InitializeCache(Dictionary<SoundType, SoundEffect> cache);
        public void Update(float dt, double totalGameTimeSeconds = 0);

        public bool Play(SoundType type, float volume = 1f, float pitch = 0f);
        public bool PlayExplosion(ExplosionScale scale = ExplosionScale.Medium, float volume = 1f, float pitch = 0f);
        public void PlayLoop(SoundType type, object sourceKey, float volume = 1f);
        public void StopLoop(object sourceKey);
        public void StopAllLoops();
        public bool IsLoopActive(object sourceKey);

        public static SoundCategory GetCategory(SoundType type);

        // Helpers de Teste Unitário
        public void SetCurrentTimeForTest(double timeSeconds);
        public void ResetForTest();
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Voice Stealing Determinístico:** Quando 20 inimigos disparam simultaneamente, o hardware de som pode sofrer clipping ou distorção. O `AudioManager` aloca até 3 vozes para armas de inimigos e 4 para impactos, interrompendo e reciclando graciosamente a voz mais antiga daquela categoria.
2. **Throttling e Debounce por Timestamp:** Se 50 projéteis colidirem no mesmo quadro, o gerenciador suprime execuções repetidas do mesmo `SoundType` que ocorram em intervalos inferiores ao limiar (ex: 25ms para `ImpactShield`), prevenindo estalos acumulados.
3. **Explosion Accretion Tracker:** Detonações que ocorrem em uma janela de 120ms recebem ganho logarítmico e detuning de pitch descendente. Ao atingir 4 detonações simultâneas, comuta automaticamente para `ExplosionMegaChain`, simulando uma explosão titânica encadeada.

---

## Exemplo Prático e Compilável

O exemplo a seguir ilustra a gestão de loops de propulsores e disparo de sons de interface via `AudioManager`:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;

namespace MyGame.Audio
{
    public class SpaceshipAudioComponent
    {
        private readonly object _thrusterLoopKey = new object();

        public void Update(GameTime gameTime, bool isBoosting)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            double totalTime = gameTime.TotalGameTime.TotalSeconds;

            // Atualiza o relógio interno e purga vozes mortas
            AudioManager.Instance.Update(dt, totalTime);

            if (isBoosting)
            {
                // Inicia ou renova o heartbeat do som de propulsor contínuo
                AudioManager.Instance.PlayLoop(SoundType.BlackHoleDrone, _thrusterLoopKey, volume: 0.65f);
            }
            else
            {
                // Encerra imediatamente o loop
                AudioManager.Instance.StopLoop(_thrusterLoopKey);
            }
        }

        public void OnClickMenuButton()
        {
            // Sons de UI possuem bypass de throttling para resposta tátil instantânea
            AudioManager.Instance.Play(SoundType.UiSelect, volume: 0.7f);
        }
    }
}
```
