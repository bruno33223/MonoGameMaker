using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NLayer;

namespace MonoGameMaker.Runtime.Audio
{
    /// <summary>
    /// Decodificador de áudio MP3 baseado em NLayer para MonoGame DesktopGL.
    /// Converte fluxos MPEG Layer III diretamente para SoundEffect (PCM 16-bit)
    /// com pipeline DSP de voz de IA de bordo:
    /// - Time-Stretching WSOLA (Synchronized Overlap-Add): acelera o áudio (1.35x) SEM alterar o pitch (zero efeito agudo/esquilo).
    /// - Equalizador balanceado (HPF 180Hz + LPF 6500Hz para clareza e presença vocal).
    /// - Modulação sintética sutil de sintetizador de bordo (Ring Modulator leve + Comb Filter).
    /// - Reverberação espacial (Schroeder Reverb) simulando a acústica da ponte de comando da nave.
    /// - SFX de ativação e finalização de rádio comunicador (Chirp + PTT Click).
    /// </summary>
    public static class Mp3AudioLoader
    {
        public const int TargetSampleRate = 44100;

        /// <summary>
        /// Carrega um arquivo MP3 padrão (ex: trilha sonora BGM) sem processamento de voz.
        /// </summary>
        public static SoundEffect? LoadSoundEffect(string filePath, out float durationSeconds)
        {
            durationSeconds = 0f;

            string fullPath = ResolveFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                using var mpeg = new MpegFile(fullPath);
                int sampleRate = mpeg.SampleRate;
                int channels = mpeg.Channels;
                durationSeconds = (float)mpeg.Duration.TotalSeconds;

                int totalSamples = (int)mpeg.Length;
                float[] floatBuffer = new float[totalSamples];
                int read = mpeg.ReadSamples(floatBuffer, 0, totalSamples);

                if (read <= 0) return null;

                byte[] pcm = new byte[read * 2];
                for (int i = 0; i < read; i++)
                {
                    float sample = Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
                    short val = (short)(sample * 32767f);
                    pcm[i * 2] = (byte)(val & 0xFF);
                    pcm[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
                }

                var channelsEnum = channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo;
                return new SoundEffect(pcm, sampleRate, channelsEnum);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Mp3AudioLoader] Erro ao carregar {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Carrega um diálogo MP3 e aplica o pipeline DSP de Voz de IA:
        /// 1. Aceleração WSOLA Time-Stretching (1.35x velocidade mantendo pitch 100% original e encorpado)
        /// 2. Equalizador Band-Pass (Corte HPF 180Hz e LPF 6500Hz para clareza e graves presentes)
        /// 3. Modulação sintética sutil de comunicador de nave
        /// 4. Reverberação espacial (Schroeder Reverb) de cockpit/ponte de comando
        /// 5. SFX de rádio: Beep de ativação antes da fala e Clique de término ao final.
        /// </summary>
        public static SoundEffect? LoadRoboticDialogueSoundEffect(string filePath, out float durationSeconds, float speedFactor = 1.35f)
        {
            durationSeconds = 0f;

            string fullPath = ResolveFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                using var mpeg = new MpegFile(fullPath);
                int srcSampleRate = mpeg.SampleRate;
                int srcChannels = mpeg.Channels;

                int totalSamples = (int)mpeg.Length;
                float[] rawBuffer = new float[totalSamples];
                int read = mpeg.ReadSamples(rawBuffer, 0, totalSamples);
                if (read <= 0) return null;

                // 1. Converte para Mono caso seja estéreo
                float[] monoSpeech;
                if (srcChannels > 1)
                {
                    int monoLen = read / srcChannels;
                    monoSpeech = new float[monoLen];
                    for (int i = 0; i < monoLen; i++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < srcChannels; c++)
                        {
                            sum += rawBuffer[i * srcChannels + c];
                        }
                        monoSpeech[i] = sum / srcChannels;
                    }
                }
                else
                {
                    monoSpeech = new float[read];
                    Array.Copy(rawBuffer, monoSpeech, read);
                }

                // 2. Aceleração de Fala via WSOLA Time-Scale Modification (Sem alterar o Pitch)
                float[] processedSpeech;
                if (speedFactor > 1.02f)
                {
                    processedSpeech = TimeStretchWSOLA(monoSpeech, speedFactor, TargetSampleRate);
                }
                else
                {
                    processedSpeech = monoSpeech;
                }

                // 3. DSP: Equalizador Band-Pass Quente (HPF 180Hz + LPF 6500Hz)
                ApplyBandPassFilter(processedSpeech, TargetSampleRate, 180f, 6500f);

                // 4. DSP: Modulação Sintética Sutil de Comunicador
                ApplyRoboticTimbre(processedSpeech, TargetSampleRate);

                // 5. DSP: Reverberação Espacial de Ponte de Comando (Cockpit Reverb)
                ApplyCockpitReverb(processedSpeech, TargetSampleRate, wetMix: 0.20f);

                // 6. Gera SFX de Ativação de Rádio e Término
                float[] introSfx = GenerateRadioIntroSfx(TargetSampleRate);
                float[] outroSfx = GenerateRadioOutroSfx(TargetSampleRate);

                // 7. Concatena [Intro SFX] + [Fala Acelerada com Pitch Natural + Reverb] + [Outro SFX]
                int totalOutLen = introSfx.Length + processedSpeech.Length + outroSfx.Length;
                float[] fullProcessed = new float[totalOutLen];

                Array.Copy(introSfx, 0, fullProcessed, 0, introSfx.Length);
                Array.Copy(processedSpeech, 0, fullProcessed, introSfx.Length, processedSpeech.Length);
                Array.Copy(outroSfx, 0, fullProcessed, introSfx.Length + processedSpeech.Length, outroSfx.Length);

                // Normalização suave e compressão Tanh anti-clipping
                NormalizeAndCompress(fullProcessed);

                durationSeconds = fullProcessed.Length / (float)TargetSampleRate;

                // 8. Converte para PCM 16-bit
                byte[] pcm = new byte[fullProcessed.Length * 2];
                for (int i = 0; i < fullProcessed.Length; i++)
                {
                    short val = (short)Math.Clamp((int)(fullProcessed[i] * 32767f), short.MinValue, short.MaxValue);
                    pcm[i * 2] = (byte)(val & 0xFF);
                    pcm[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
                }

                return new SoundEffect(pcm, TargetSampleRate, AudioChannels.Mono);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Mp3AudioLoader] Erro ao processar diálogo de IA {filePath}: {ex.Message}");
                return null;
            }
        }

        private static string ResolveFullPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;

            // Direct check
            string fullPath = Path.GetFullPath(filePath);
            if (File.Exists(fullPath)) return fullPath;

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            fullPath = Path.Combine(appDir, filePath);
            if (File.Exists(fullPath)) return fullPath;

            string fileName = Path.GetFileName(filePath);
            string[] candidateDirs = new[]
            {
                Path.Combine(appDir, "Content", "Audio", "Dialogues", "Tutorial"),
                Path.Combine(appDir, "Content", "Audio", "Dialogues", "Story"),
                Path.Combine(appDir, "Content", "Audio", "Dialogues", "Deaths"),
                Path.Combine(appDir, "Content", "Audio", "Dialogues"),
                Path.Combine(appDir, "Content", "Audio", "BGM"),
                Path.Combine(appDir, "Content", "Audio"),
                Path.Combine(appDir, "dialogos"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Audio", "Dialogues", "Tutorial"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Audio", "Dialogues", "Story"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Audio", "Dialogues", "Deaths"),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", "Audio", "Dialogues"),
                Path.Combine(Directory.GetCurrentDirectory(), "dialogos")
            };

            foreach (var dir in candidateDirs)
            {
                if (Directory.Exists(dir))
                {
                    string cand = Path.Combine(dir, fileName);
                    if (File.Exists(cand)) return cand;
                }
            }

            return Path.Combine(appDir, filePath);
        }

        #region WSOLA Time-Stretching (Aceleração com Pitch Constante)

        /// <summary>
        /// Implementação de WSOLA (Waveform Similarity Overlap-Add) para acelerar a fala
        /// preservando 100% da afinação/frequência fundamental (pitch) original sem distorção.
        /// </summary>
        private static float[] TimeStretchWSOLA(float[] input, float speedFactor, int sampleRate)
        {
            if (input.Length < sampleRate * 0.1f || speedFactor <= 1.0f)
                return input;

            int windowSize = (int)(sampleRate * 0.028f); // Janela de 28ms (~1234 amostras)
            int overlap = windowSize / 2;
            int synHop = overlap;
            int maxSearch = (int)(sampleRate * 0.012f); // Busca de fase de 12ms

            int estimatedOutLen = (int)(input.Length / speedFactor) + windowSize;
            float[] output = new float[estimatedOutLen];

            // Copia o primeiro bloco inicial diretamente
            int initialCopy = Math.Min(windowSize, input.Length);
            Array.Copy(input, 0, output, 0, initialCopy);

            int outPos = synHop;
            float inNominalPos = synHop * speedFactor;

            while (inNominalPos + windowSize + maxSearch < input.Length && outPos + windowSize < output.Length)
            {
                int nominalIdx = (int)inNominalPos;
                int searchStart = Math.Max(0, nominalIdx - maxSearch);
                int searchEnd = Math.Min(input.Length - windowSize, nominalIdx + maxSearch);

                // Encontra a melhor correlação de forma de onda (alinhamento de fase de pitch)
                int bestOffset = nominalIdx;
                float bestCorr = float.MinValue;

                for (int cand = searchStart; cand <= searchEnd; cand += 2) // Step de 2 para otimização
                {
                    float corr = 0f;
                    for (int k = 0; k < overlap; k += 4) // Amostragem rápida de correlação cruzada
                    {
                        corr += output[outPos + k] * input[cand + k];
                    }

                    if (corr > bestCorr)
                    {
                        bestCorr = corr;
                        bestOffset = cand;
                    }
                }

                // Overlap-Add com janela de Hann
                for (int k = 0; k < overlap; k++)
                {
                    float w = 0.5f * (1f - MathF.Cos(MathF.PI * k / overlap)); // Fade-in Hann
                    output[outPos + k] = output[outPos + k] * (1f - w) + input[bestOffset + k] * w;
                }

                // Copia a porção restante do frame
                int remaining = windowSize - overlap;
                Array.Copy(input, bestOffset + overlap, output, outPos + overlap, remaining);

                outPos += synHop;
                inNominalPos += synHop * speedFactor;
            }

            // Trunca para o tamanho útil gerado
            int finalLength = Math.Max(synHop, outPos + overlap);
            float[] trimmed = new float[finalLength];
            Array.Copy(output, 0, trimmed, 0, Math.Min(finalLength, output.Length));
            return trimmed;
        }

        #endregion

        #region Filtros DSP de Voz de IA e Reverb

        /// <summary>
        /// Aplica filtro Passa-Faixa Bi-quad Butterworth (HPF 180Hz + LPF 6500Hz).
        /// </summary>
        private static void ApplyBandPassFilter(float[] buffer, float sampleRate, float lowCutHz, float highCutHz)
        {
            // Biquad High-Pass (180Hz, Q=0.707)
            float w0H = 2f * MathF.PI * lowCutHz / sampleRate;
            float cosH = MathF.Cos(w0H);
            float sinH = MathF.Sin(w0H);
            float alphaH = sinH / (2f * 0.7071f);

            float b0H = (1f + cosH) / 2f;
            float b1H = -(1f + cosH);
            float b2H = (1f + cosH) / 2f;
            float a0H = 1f + alphaH;
            float a1H = -2f * cosH;
            float a2H = 1f - alphaH;

            // Biquad Low-Pass (6500Hz, Q=0.707)
            float w0L = 2f * MathF.PI * highCutHz / sampleRate;
            float cosL = MathF.Cos(w0L);
            float sinL = MathF.Sin(w0L);
            float alphaL = sinL / (2f * 0.7071f);

            float b0L = (1f - cosL) / 2f;
            float b1L = 1f - cosL;
            float b2L = (1f - cosL) / 2f;
            float a0L = 1f + alphaL;
            float a1L = -2f * cosL;
            float a2L = 1f - alphaL;

            b0H /= a0H; b1H /= a0H; b2H /= a0H; a1H /= a0H; a2H /= a0H;
            b0L /= a0L; b1L /= a0L; b2L /= a0L; a1L /= a0L; a2L /= a0L;

            float x1H = 0, x2H = 0, y1H = 0, y2H = 0;
            float x1L = 0, x2L = 0, y1L = 0, y2L = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                float x = buffer[i];

                // High-Pass
                float yH = b0H * x + b1H * x1H + b2H * x2H - a1H * y1H - a2H * y2H;
                x2H = x1H; x1H = x;
                y2H = y1H; y1H = yH;

                // Low-Pass
                float yL = b0L * yH + b1L * x1L + b2L * x2L - a1L * y1L - a2L * y2L;
                x2L = x1L; x1L = yH;
                y2L = y1L; y1L = yL;

                buffer[i] = yL;
            }
        }

        /// <summary>
        /// Aplica modulação sintética de sintetizador de comunicador sutil (Ring Modulator a 32Hz leve + Comb Delay).
        /// </summary>
        private static void ApplyRoboticTimbre(float[] buffer, float sampleRate)
        {
            int delaySamples = (int)(sampleRate * 0.008f); // 8ms de micro-delay
            float[] delayLine = new float[delaySamples];
            int delayIdx = 0;

            float ringFreq = 32.0f; // 32Hz modulação sub-harmônica suave
            float ringMix = 0.08f;  // 8% de modulação em anel (mantém a voz natural e limpa)
            float combFeedback = 0.15f;

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / sampleRate;
                float original = buffer[i];

                // Ring Modulation sutil
                float carrier = MathF.Sin(2f * MathF.PI * ringFreq * t);
                float ringed = original * carrier;
                float combined = original * (1f - ringMix) + ringed * ringMix;

                // Comb Filter sutil
                float delayed = delayLine[delayIdx];
                float outSample = combined + delayed * combFeedback;
                delayLine[delayIdx] = combined + delayed * 0.10f;
                delayIdx = (delayIdx + 1) % delaySamples;

                buffer[i] = outSample;
            }
        }

        /// <summary>
        /// Aplica reverberação de ponte de comando / cockpit espacial usando algoritmo Schroeder Reverb.
        /// (4 Comb Filters em paralelo com All-Pass filter em série).
        /// </summary>
        private static void ApplyCockpitReverb(float[] buffer, float sampleRate, float wetMix = 0.20f)
        {
            // Tamanhos de linha de atraso (amostras com números primos para evitar ressonâncias estridentes)
            int[] combLengths = { 1116, 1356, 1422, 1617 };
            float[][] combBuffers = new float[4][];
            int[] combIndices = new int[4];
            float combFeedback = 0.70f;
            float combDamping = 0.25f;
            float[] combFilterState = new float[4];

            for (int c = 0; c < 4; c++)
            {
                combBuffers[c] = new float[combLengths[c]];
                combIndices[c] = 0;
            }

            // All-Pass Filter para difusão
            int allPassLength = 225;
            float[] allPassBuffer = new float[allPassLength];
            int allPassIndex = 0;
            float allPassGain = 0.5f;

            float dryMix = 1.0f - wetMix;

            for (int i = 0; i < buffer.Length; i++)
            {
                float dry = buffer[i];
                float combSum = 0f;

                // 4 Comb Filters em paralelo
                for (int c = 0; c < 4; c++)
                {
                    int idx = combIndices[c];
                    float delayed = combBuffers[c][idx];

                    // Low-Pass Damping no feedback
                    combFilterState[c] = delayed * (1f - combDamping) + combFilterState[c] * combDamping;
                    combBuffers[c][idx] = dry + combFilterState[c] * combFeedback;
                    combIndices[c] = (idx + 1) % combLengths[c];

                    combSum += delayed;
                }

                float wet = combSum * 0.25f;

                // All-Pass Filter em série para dispersão e densidade de cauda
                float apDelayed = allPassBuffer[allPassIndex];
                float apIn = wet - apDelayed * allPassGain;
                float apOut = apDelayed + apIn * allPassGain;
                allPassBuffer[allPassIndex] = apIn;
                allPassIndex = (allPassIndex + 1) % allPassLength;

                // Mix final (Dry / Wet)
                buffer[i] = dry * dryMix + apOut * wetMix;
            }
        }

        /// <summary>
        /// Gera SFX de ativação de rádio (duplo chirp suave + sopro curto de squelch).
        /// </summary>
        private static float[] GenerateRadioIntroSfx(float sampleRate)
        {
            int samples = (int)(sampleRate * 0.080f); // 80ms
            float[] sfx = new float[samples];
            var rng = new Random(1337);

            for (int i = 0; i < samples; i++)
            {
                float t = i / sampleRate;
                float chirp = 0f;

                if (t < 0.030f)
                {
                    // Beep 1 (1400Hz - mais grave e agradável)
                    float env = MathF.Sin(t / 0.030f * MathF.PI);
                    chirp += MathF.Sin(2f * MathF.PI * 1400f * t) * env * 0.25f;
                }
                else if (t < 0.065f)
                {
                    // Beep 2 (1900Hz)
                    float env = MathF.Sin((t - 0.030f) / 0.035f * MathF.PI);
                    chirp += MathF.Sin(2f * MathF.PI * 1900f * t) * env * 0.28f;
                }

                // Ruído de rádio squelch de fundo muito suave
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
                float noiseEnv = MathF.Exp(-t * 30f);

                sfx[i] = chirp + noise * noiseEnv;
            }

            return sfx;
        }

        /// <summary>
        /// Gera SFX de término de rádio (clique mecânico discreto de liberação de PTT).
        /// </summary>
        private static float[] GenerateRadioOutroSfx(float sampleRate)
        {
            int samples = (int)(sampleRate * 0.060f); // 60ms
            float[] sfx = new float[samples];
            var rng = new Random(4242);

            for (int i = 0; i < samples; i++)
            {
                float t = i / sampleRate;

                // Squelch e clique discreto
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
                float noiseEnv = MathF.Exp(-t * 60f);

                float clickFreq = MathHelper.Lerp(800f, 250f, t / 0.060f);
                float click = MathF.Sin(2f * MathF.PI * clickFreq * t) * MathF.Exp(-t * 90f) * 0.30f;

                sfx[i] = noise * noiseEnv + click;
            }

            return sfx;
        }

        private static void NormalizeAndCompress(float[] buffer)
        {
            float maxPeak = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float abs = MathF.Abs(buffer[i]);
                if (abs > maxPeak) maxPeak = abs;
            }

            float gain = maxPeak > 0.001f ? (0.92f / maxPeak) : 1.0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float scaled = buffer[i] * gain;
                buffer[i] = MathF.Tanh(scaled); // Soft saturation analógica
            }
        }

        #endregion
    }
}
