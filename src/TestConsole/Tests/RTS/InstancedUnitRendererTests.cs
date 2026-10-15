using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Graphics3D;

namespace TestConsole.Tests.RTS
{
    public static class InstancedUnitRendererTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE RTS 6/6] Executando InstancedUnitRendererTests (TEST-INST-01)...");

            Test_TEST_INST_01(ref passCount, ref totalCount);
        }

        /// <summary>
        /// TEST-INST-01: Validação exata de 64 bytes em Marshal.SizeOf<UnitInstanceData>() e integridade de empacotamento.
        /// </summary>
        private static void Test_TEST_INST_01(ref int passCount, ref int totalCount)
        {
            totalCount++;

            // 1. Validação estrita do tamanho bruto (64 bytes = 4x Vector4 de 16 bytes = 1 linha de cache)
            int structSize = Marshal.SizeOf<UnitInstanceData>();
            if (structSize != 64)
            {
                throw new Exception($"[TEST-INST-01] Tamanho incorreto da struct UnitInstanceData: esperado 64 bytes, obtido {structSize}");
            }

            // 2. Criação de instância de teste com valores específicos
            var original = new UnitInstanceData
            {
                TransformData = new Vector4(123.456f, 789.012f, 1.5707963f, 2.5f),
                AtlasUVRect   = new Vector4(0.125f, 0.25f, 0.0625f, 0.0625f),
                TeamColor     = new Vector4(0.2f, 0.4f, 0.6f, 1.0f),
                VisualFlags   = new Vector4(1.0f, 0.85f, 0.0f, 0.0f)
            };

            // 3. Serialização de memória bruta para byte[64]
            byte[] rawBytes = new byte[64];
            MemoryMarshal.Write(rawBytes.AsSpan(), in original);

            // 4. Desserialização de volta via MemoryMarshal.Read
            var reconstituted = MemoryMarshal.Read<UnitInstanceData>(rawBytes.AsSpan());

            // 5. Comparação precisa float a float
            if (original.TransformData != reconstituted.TransformData)
                throw new Exception($"[TEST-INST-01] Memória corrompida em TransformData: original {original.TransformData} != reconstituído {reconstituted.TransformData}");

            if (original.AtlasUVRect != reconstituted.AtlasUVRect)
                throw new Exception($"[TEST-INST-01] Memória corrompida em AtlasUVRect: original {original.AtlasUVRect} != reconstituído {reconstituted.AtlasUVRect}");

            if (original.TeamColor != reconstituted.TeamColor)
                throw new Exception($"[TEST-INST-01] Memória corrompida em TeamColor: original {original.TeamColor} != reconstituído {reconstituted.TeamColor}");

            if (original.VisualFlags != reconstituted.VisualFlags)
                throw new Exception($"[TEST-INST-01] Memória corrompida em VisualFlags: original {original.VisualFlags} != reconstituído {reconstituted.VisualFlags}");

            // 6. Teste de submissão headless no InstancedUnitRenderer
            using var renderer = new InstancedUnitRenderer(graphicsDevice: null);
            renderer.BeginBatch();
            for (int i = 0; i < 500; i++)
            {
                renderer.Submit(
                    worldPos: new Vector2(i * 2f, i * 3f),
                    rotation: 0.5f,
                    scale: 1.0f,
                    atlasUV: new Vector4(0f, 0f, 1f, 1f),
                    teamColor: Color.Red,
                    isSelected: (i % 2 == 0),
                    healthRatio: 1.0f);
            }

            if (renderer.CurrentInstanceCount != 500)
                throw new Exception($"[TEST-INST-01] InstancedUnitRenderer contagem incorreta: esperado 500, obtido {renderer.CurrentInstanceCount}");

            passCount++;
            Console.WriteLine("  -> [TEST-INST-01] Validação exata de 64 bytes em UnitInstanceData e integridade de empacotamento: PASS");
        }
    }
}
