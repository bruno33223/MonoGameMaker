# `SoundSynthesizer` Manual da API

O `SoundSynthesizer` é o motor procedural de síntese digital de áudio (DSP) por software do MonoGameMaker. Desenvolvido para dispensar dependências de bibliotecas de som pesadas e arquivos WAV pré-gravados para efeitos balísticos e de interface, ele sintetiza ondas senoidais, ruído rosa, ruído marrom, sub-bass rumbles e modulações analógicas diretamente em buffers PCM de 16-bit 44.1kHz compatíveis com `SoundEffect` do MonoGame.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Audio
{
    public class SoundSynthesizer : IDisposable
    {
        public const int SampleRate = 44100;

        public SoundSynthesizer();

        public SoundEffect? GetEffect(SoundType type);
        public void Play(SoundType type, float volume = 1f, float pitch = 0f);
        public void PlayExplosion(ExplosionScale scale = ExplosionScale.Medium, float volume = 1f);
        public void PlayLoop(SoundType type, object sourceKey, float volume = 1f);
        public void StopLoop(object sourceKey);
        public void StopAllLoops();
        public void Update(float dt, double totalGameTimeSeconds = 0);
        public void Dispose();

        // Pipeline DSP Estático
        public static byte[] GenerateRawPCM(SoundType type);
        public static byte[] ConvertFloatsToPCM16(float[] samples);
        public static void ApplyTanhMastering(float[] buffer, float drive, float masterGain = 0.85f);
        public static void ApplyEnvelope(float[] buffer, float sampleRate, float attackSec, float decaySec, bool strictTailZero = true);

        // Métodos de Conveniência
        public void PlayCriticalHit(float volume = 0.85f, float pitch = 0f);
        public void PlayUiHover(float volume = 0.40f);
        public void PlayUiSelect(float volume = 0.60f);
        public void PlayUiBack(float volume = 0.50f);
        public void PlayPlayerChargeUp(float volume = 0.75f, float pitch = 0f);
        public void PlayPlayerChargeRelease(float volume = 0.85f, float pitch = 0f);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Geração Instantânea de Efeitos Sonoros:** Evita a necessidade de carregar centenas de assets de áudio em disco para lasers, disparos pesados, explosões e interfaces. Ao iniciar o jogo, o `SoundSynthesizer` computa todos os tipos de áudio proceduralmente em frações de segundo.
2. **Mastering Analógico e Proteção Anti-Pop:** Todas as amostras sintetizadas passam por saturação suave via $\tanh(x)$, limitando o pico máximo a $[-1.0, 1.0]$ e aplicando fade-out estrito nos últimos 5% de cauda (`strictTailZero`), prevenindo cliques ou estalos em caixas de som.
3. **Tolerância Headless para Testes e CI/CD:** O método `GenerateRawPCM(SoundType)` retorna arrays puros de bytes `byte[]` que podem ser inspecionados em ambientes Linux headless (`dotnet test`) sem hardware de áudio ou driver ALSA instalado.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a inicialização do sintetizador e a emissão de disparos e detonações em resposta a eventos de jogo:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;

namespace MyGame.Audio
{
    public class AudioDemo
    {
        private readonly SoundSynthesizer _synth;

        public AudioDemo()
        {
            // Instancia o sintetizador e pré-calcula as formas de onda DSP
            _synth = new SoundSynthesizer();
        }

        public void OnPlayerShoot(bool isCharged)
        {
            if (isCharged)
            {
                // Disparo de canhão de plasma pesado com ganho encorpado
                _synth.Play(SoundType.PlayerShotHeavy, volume: 0.9f, pitch: -0.05f);
            }
            else
            {
                // Disparo ágil de laser primário
                _synth.Play(SoundType.PlayerShotLight, volume: 0.6f, pitch: 0f);
            }
        }

        public void OnEnemyDestroyed(bool isBoss)
        {
            if (isBoss)
            {
                // Detonação massiva com sub-bass e estilhaço metálico
                _synth.PlayExplosion(ExplosionScale.Heavy, volume: 1.0f);
            }
            else
            {
                // Detonação convencional
                _synth.PlayExplosion(ExplosionScale.Small, volume: 0.7f);
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            double totalSec = gameTime.TotalGameTime.TotalSeconds;
            _synth.Update(dt, totalSec);
        }

        public void Shutdown()
        {
            _synth.Dispose();
        }
    }
}
```
