# `Mp3AudioLoader` Manual da API

O `Mp3AudioLoader` é o decodificador puramente gerenciado em C# para arquivos MP3 no MonoGameMaker, construído sobre a biblioteca `NLayer`. Ele elimina qualquer necessidade de codecs externos do sistema operacional ou DLLs nativas do Windows Media Foundation, operando de forma idêntica em Linux, macOS e Windows, além de incorporar um pipeline DSP completo de rádio de inteligência artificial de bordo com time-stretching e reverberação de cockpit.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Audio
{
    public static class Mp3AudioLoader
    {
        public const int TargetSampleRate = 44100;

        /// <summary>
        /// Carrega um arquivo MP3 padrão (trilhas musicais BGM) e converte em SoundEffect PCM 16-bit.
        /// </summary>
        public static SoundEffect? LoadSoundEffect(string filePath, out float durationSeconds);

        /// <summary>
        /// Carrega um diálogo MP3 e aplica o pipeline DSP de Voz de IA:
        /// - Time-Stretching WSOLA (1.35x de velocidade com pitch inalterado)
        /// - Equalizador passa-faixa (HPF 180Hz + LPF 6500Hz)
        /// - Modulação sintética sutil de comunicador
        /// - Reverberação espacial de cabine (Cockpit Schroeder Reverb)
        /// - Beeps de ativação e cliques de liberação de rádio PTT.
        /// </summary>
        public static SoundEffect? LoadRoboticDialogueSoundEffect(string filePath, out float durationSeconds, float speedFactor = 1.35f);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Trilhas Sonoras Multiplataforma sem Bloqueio de Plataforma:** O MonoGame nativo depende frequentemente de WMF no Windows ou GStreamer no Linux para decodificar MP3/WMA, gerando falhas em distribuições Linux enxutas. O `Mp3AudioLoader` utiliza C# puro para converter MP3 em buffers PCM 16-bit estáveis.
2. **Pipeline de Rádio de IA de Bordo:** Para jogos espaciais e de ficção científica com transmissões de rádio ou voz de sintetizador, o método `LoadRoboticDialogueSoundEffect` acelera a leitura de diálogos mantendo o timbre natural, simulando a acústica de uma ponte de comando.
3. **Dynamic Music Ducking:** Permite obter a duração em segundos da fala para aplicar ducking de 70% no volume da música (`AudioManager.Instance.MusicVolume = 0.30f`) enquanto a transmissão estiver ativa.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra o carregamento e reprodução de uma transmissão de rádio com controle de ducking da música de combate:

```csharp
using Microsoft.Xna.Framework.Audio;
using MonoGameMaker.Runtime.Audio;

namespace MyGame.Audio
{
    public class RadioDialogueController
    {
        private SoundEffectInstance? _currentSpeechInstance;
        private float _speechTimer = 0f;
        private float _speechDuration = 0f;

        public void PlayAITransmission(string mp3RelativePath)
        {
            // Decodifica o MP3 e processa com WSOLA + Cockpit Reverb
            var soundEffect = Mp3AudioLoader.LoadRoboticDialogueSoundEffect(mp3RelativePath, out _speechDuration, speedFactor: 1.35f);
            if (soundEffect != null)
            {
                _currentSpeechInstance = soundEffect.CreateInstance();
                _currentSpeechInstance.Volume = 0.95f;
                _currentSpeechInstance.Play();

                _speechTimer = _speechDuration;

                // Ducking da música de combate para destacar a voz do operador
                AudioManager.Instance.MusicVolume = 0.30f;
            }
        }

        public void Update(float dt)
        {
            if (_speechTimer > 0f)
            {
                _speechTimer -= dt;
                if (_speechTimer <= 0f)
                {
                    // Restaura volume normal da música ao término da fala
                    AudioManager.Instance.MusicVolume = 1.0f;
                }
            }
        }
    }
}
```
