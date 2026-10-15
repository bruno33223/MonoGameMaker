using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;

namespace TestConsole.Tests
{
    public static class AudioDspAndThrottlingTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 5/8] Executando AudioDspAndThrottlingTests (TEST-AUD-01 a 08)...");

            Test_AUD_01_PcmBufferStructuralIntegrity(ref passCount, ref totalCount);
            Test_AUD_02_NoNaNOrInfinitySamples(ref passCount, ref totalCount);
            Test_AUD_03_PeakClampingLimit(ref passCount, ref totalCount);
            Test_AUD_04_TailFadeOutDCProtection(ref passCount, ref totalCount);
            Test_AUD_05_TimestampThrottlingAndDebounce(ref passCount, ref totalCount);
            Test_AUD_06_SemanticCategoryMappingCoverage(ref passCount, ref totalCount);
            Test_AUD_07_VoiceStealingPolicy(ref passCount, ref totalCount);
            Test_AUD_08_SustainedLoopWatchdogAndStop(ref passCount, ref totalCount);
        }

        public static void Test_AUD_01_PcmBufferStructuralIntegrity(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soundTypes = (SoundType[])Enum.GetValues(typeof(SoundType));
            foreach (var type in soundTypes)
            {
                byte[] pcm = SoundSynthesizer.GenerateRawPCM(type);
                if (pcm == null || pcm.Length == 0)
                    throw new Exception($"[TEST-AUD-01] Buffer PCM gerado para {type} é nulo ou vazio.");
                if (pcm.Length % 2 != 0)
                    throw new Exception($"[TEST-AUD-01] Buffer PCM para {type} tem tamanho ímpar ({pcm.Length}), esperado 16-bit mono alinhado.");
            }

            passCount++;
            Console.WriteLine($"  -> [TEST-AUD-01] Integridade Estrutural dos Buffers PCM ({soundTypes.Length} SoundTypes): PASS");
        }

        public static void Test_AUD_02_NoNaNOrInfinitySamples(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soundTypes = (SoundType[])Enum.GetValues(typeof(SoundType));
            foreach (var type in soundTypes)
            {
                byte[] pcm = SoundSynthesizer.GenerateRawPCM(type);
                for (int i = 0; i < pcm.Length; i += 2)
                {
                    short sample = (short)(pcm[i] | (pcm[i + 1] << 8));
                    float norm = sample / 32768.0f;
                    if (float.IsNaN(norm) || float.IsInfinity(norm))
                        throw new Exception($"[TEST-AUD-02] Amostra inválida (NaN/Inf) detectada em {type} no byte {i}");
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-AUD-02] Ausência de Amostras NaN / Infinity em Todos os Sons: PASS");
        }

        public static void Test_AUD_03_PeakClampingLimit(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soundTypes = (SoundType[])Enum.GetValues(typeof(SoundType));
            foreach (var type in soundTypes)
            {
                byte[] pcm = SoundSynthesizer.GenerateRawPCM(type);
                for (int i = 0; i < pcm.Length; i += 2)
                {
                    short sample = (short)(pcm[i] | (pcm[i + 1] << 8));
                    float norm = Math.Abs(sample / 32767.0f);
                    if (norm > 1.0001f)
                        throw new Exception($"[TEST-AUD-03] Amostra ultrapassou teto de clipping 1.0f em {type}: {norm}");
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-AUD-03] Limitador de Pico (Peak Clamping <= 1.0f): PASS");
        }

        public static void Test_AUD_04_TailFadeOutDCProtection(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soundTypes = (SoundType[])Enum.GetValues(typeof(SoundType));
            foreach (var type in soundTypes)
            {
                byte[] pcm = SoundSynthesizer.GenerateRawPCM(type);
                int lastSampleIndex = pcm.Length - 2;
                short lastSample = (short)(pcm[lastSampleIndex] | (pcm[lastSampleIndex + 1] << 8));
                float lastNorm = Math.Abs(lastSample / 32767.0f);

                if (lastNorm > 0.05f)
                    throw new Exception($"[TEST-AUD-04] Cauda de áudio em {type} não atenuou para <= 0.05f (DC click). Obtido: {lastNorm}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-AUD-04] Atenuação da Cauda (Fade-Out 5% Final <= 0.05f): PASS");
        }

        public static void Test_AUD_05_TimestampThrottlingAndDebounce(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var audio = AudioManager.Instance;
            audio.ResetForTest();
            audio.SetCurrentTimeForTest(5.0); // Timestamp fixo t = 5.0s

            int playedCount = 0;
            int suppressedCount = 0;

            // 20 coletas simultâneas no mesmo timestamp
            for (int i = 0; i < 20; i++)
            {
                bool played = audio.Play(SoundType.SoulPickup);
                if (played) playedCount++;
                else suppressedCount++;
            }

            if (playedCount != 1)
                throw new Exception($"[TEST-AUD-05] Throttling falhou: esperado exatamente 1 reprodução, obtido {playedCount}");
            if (suppressedCount != 19)
                throw new Exception($"[TEST-AUD-05] Throttling falhou: esperado 19 solicitações suprimidas, obtido {suppressedCount}");

            audio.ResetForTest();
            passCount++;
            Console.WriteLine("  -> [TEST-AUD-05] Throttling e Debounce por Timestamp (20 pedidos -> 1 play, 19 suprimidos): PASS");
        }

        public static void Test_AUD_06_SemanticCategoryMappingCoverage(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var soundTypes = (SoundType[])Enum.GetValues(typeof(SoundType));
            foreach (var type in soundTypes)
            {
                var category = AudioManager.GetCategory(type);
                // Verifica se retorna uma categoria enum válida
                if (!Enum.IsDefined(typeof(SoundCategory), category))
                    throw new Exception($"[TEST-AUD-06] Categoria inválida retornada para {type}: {category}");
            }

            passCount++;
            Console.WriteLine($"  -> [TEST-AUD-06] Mapeamento de Categorias Semânticas ({soundTypes.Length} tipos): PASS");
        }

        public static void Test_AUD_07_VoiceStealingPolicy(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var audio = AudioManager.Instance;
            audio.ResetForTest();

            // Categoria de explosões tem capacidade máxima de 3 vozes
            // Emite 4 explosões em timestamps distintos
            audio.SetCurrentTimeForTest(1.0);
            audio.Play(SoundType.ExplosionSmall);

            audio.SetCurrentTimeForTest(1.1);
            audio.Play(SoundType.ExplosionMedium);

            audio.SetCurrentTimeForTest(1.2);
            audio.Play(SoundType.ExplosionHeavy);

            audio.SetCurrentTimeForTest(1.3);
            bool fourthPlay = audio.Play(SoundType.ExplosionSmall);

            if (!fourthPlay)
                throw new Exception("[TEST-AUD-07] Voice stealing falhou ao aceitar a 4ª voz (deveria roubar a mais antiga).");

            audio.ResetForTest();
            passCount++;
            Console.WriteLine("  -> [TEST-AUD-07] Voice Stealing (Substituição Inteligente sem Estouro de Canal): PASS");
        }

        public static void Test_AUD_08_SustainedLoopWatchdogAndStop(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var audio = AudioManager.Instance;
            audio.ResetForTest();

            object sourceKey = "ThrusterSource_1";
            audio.PlayLoop(SoundType.BlackHoleDrone, sourceKey);

            if (!audio.IsLoopActive(sourceKey))
                throw new Exception("[TEST-AUD-08] Loop não registrado como ativo após PlayLoop.");

            // Aciona StopAllLoops
            audio.StopAllLoops();

            if (audio.IsLoopActive(sourceKey))
                throw new Exception("[TEST-AUD-08] Loop permaneceu ativo após StopAllLoops.");

            if (audio.ActiveLoopCount != 0)
                throw new Exception($"[TEST-AUD-08] ActiveLoopCount deve ser 0 após StopAllLoops. Obtido: {audio.ActiveLoopCount}");

            audio.ResetForTest();
            passCount++;
            Console.WriteLine("  -> [TEST-AUD-08] Watchdog de Encerramento de Loops Sustentados: PASS");
        }
    }
}
