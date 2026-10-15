using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Spatial;

namespace TestConsole.Tests.RTS
{
    public static class SpatialHashGridTests
    {
        public static void RunAll(ref int passCount, ref int totalCount, out long allocatedBytes)
        {
            Console.WriteLine("\n[SUÍTE RTS 1/6] Executando SpatialHashGridTests (TEST-SPATIAL-01 a 04)...");
            allocatedBytes = 0;

            Test_TEST_SPATIAL_01(ref passCount, ref totalCount);
            Test_TEST_SPATIAL_02(ref passCount, ref totalCount);
            Test_TEST_SPATIAL_03(ref passCount, ref totalCount);
            Test_TEST_SPATIAL_04(ref passCount, ref totalCount, out allocatedBytes);
        }

        /// <summary>
        /// TEST-SPATIAL-01: Inserção, atualização e remoção determinística.
        /// </summary>
        private static void Test_TEST_SPATIAL_01(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var grid = new SpatialHashGrid(1024, 1024, 32.0f, 256);

            // 1. Inserir unidades 0 e 1 na mesma célula (0, 0) -> cellIndex 0
            grid.Insert(0, new Vector2(10f, 10f));
            grid.Insert(1, new Vector2(15f, 15f));

            // Unidade 2 em célula diferente (64, 64) -> cellIndex (2*32 + 2) = 66
            grid.Insert(2, new Vector2(64f, 64f));

            // Verificar se cellHead aponta para a última inserida (1) e nextEntity encadeia a anterior (0)
            int cell0 = 0;
            if (grid.GetCellHead(cell0) != 1)
                throw new Exception($"[TEST-SPATIAL-01] cellHead esperado 1, obtido {grid.GetCellHead(cell0)}");

            if (grid.GetNextEntity(1) != 0)
                throw new Exception($"[TEST-SPATIAL-01] nextEntity de 1 esperado 0, obtido {grid.GetNextEntity(1)}");

            if (grid.GetNextEntity(0) != -1)
                throw new Exception($"[TEST-SPATIAL-01] nextEntity de 0 esperado -1, obtido {grid.GetNextEntity(0)}");

            // 2. Remoção de entidade 1
            grid.Remove(1);
            if (grid.GetCellHead(cell0) != 0)
                throw new Exception($"[TEST-SPATIAL-01] Após remover 1, cellHead esperado 0, obtido {grid.GetCellHead(cell0)}");

            // 3. Atualização de posição da unidade 0 para outra célula
            grid.UpdatePosition(0, new Vector2(10f, 10f), new Vector2(100f, 100f));
            if (grid.GetCellHead(cell0) != -1)
                throw new Exception($"[TEST-SPATIAL-01] Célula antiga deveria estar vazia (-1), obtido {grid.GetCellHead(cell0)}");

            // 4. Limpeza total Clear()
            grid.Clear();
            for (int c = 0; c < grid.CellColumns * grid.CellRows; c++)
            {
                if (grid.GetCellHead(c) != -1)
                    throw new Exception($"[TEST-SPATIAL-01] Clear() falhou na célula {c}, head não é -1");
            }

            // 5. Reinserção
            grid.Insert(5, new Vector2(200f, 200f));
            if (grid.GetEntityCell(5) < 0)
                throw new Exception("[TEST-SPATIAL-01] Falha ao reinserir entidade após Clear()");

            passCount++;
            Console.WriteLine("  -> [TEST-SPATIAL-01] Inserção, atualização e remoção determinística: PASS");
        }

        /// <summary>
        /// TEST-SPATIAL-02: Busca por raio (FindNeighbors) com filtro euclidiano exato (d <= R).
        /// </summary>
        private static void Test_TEST_SPATIAL_02(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var grid = new SpatialHashGrid(1024, 1024, 32.0f, 64);
            Vector2 center = new Vector2(100f, 100f);
            float radius = 30.0f;

            // Insere 1 entidade central (id 0)
            grid.Insert(0, center);

            // 5 entidades dentro do raio (ids 1..5, distâncias de 5, 10, 15, 20, 28)
            float[] insideDists = { 5f, 10f, 15f, 20f, 28f };
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = center + new Vector2(insideDists[i], 0f);
                grid.Insert(i + 1, pos);
            }

            // 5 entidades fora do raio (ids 6..10, distâncias de 31, 35, 45, 60, 80)
            float[] outsideDists = { 31f, 35f, 45f, 60f, 80f };
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = center + new Vector2(outsideDists[i], 0f);
                grid.Insert(i + 6, pos);
            }

            Span<int> results = stackalloc int[32];
            int count = grid.FindNeighbors(center, radius, results, ignoreEntityId: 0);

            if (count != 5)
                throw new Exception($"[TEST-SPATIAL-02] Esperado exatamente 5 vizinhos dentro de R={radius}, obtido {count}");

            for (int i = 0; i < count; i++)
            {
                int id = results[i];
                float dist = Vector2.Distance(center, grid.Positions[id]);
                if (dist > radius + RtsTestMath.EpsilonPosition)
                {
                    throw new Exception($"[TEST-SPATIAL-02] Entidade {id} com dist={dist} excede raio {radius}");
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-SPATIAL-02] Busca por raio (FindNeighbors) com filtro euclidiano exato (d <= R): PASS");
        }

        /// <summary>
        /// TEST-SPATIAL-03: Busca por AABB (QueryBox) retornando somente entidades contidas na caixa.
        /// </summary>
        private static void Test_TEST_SPATIAL_03(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var grid = new SpatialHashGrid(1024, 1024, 32.0f, 64);
            var box = new RectangleF(100f, 100f, 200f, 200f); // Min(100,100), Max(300,300)

            // Povoar 20 entidades: 8 dentro da caixa, 12 fora
            int insideCount = 0;
            for (int i = 0; i < 20; i++)
            {
                Vector2 pos = (i < 8)
                    ? new Vector2(120f + (i * 20f), 120f + (i * 20f)) // Dentro: (120,120) a (260,260)
                    : new Vector2(50f + (i * 30f), 400f);             // Fora (y=400 > 300)

                grid.Insert(i, pos);
                if (box.Contains(pos)) insideCount++;
            }

            if (insideCount != 8)
                throw new Exception($"[TEST-SPATIAL-03] Configuração de teste inconsistente: insideCount={insideCount}");

            Span<int> results = stackalloc int[32];
            int count = grid.QueryBox(box, results);

            if (count != 8)
                throw new Exception($"[TEST-SPATIAL-03] QueryBox retornou {count} entidades, esperado 8");

            for (int i = 0; i < count; i++)
            {
                int id = results[i];
                if (!box.Contains(grid.Positions[id]))
                    throw new Exception($"[TEST-SPATIAL-03] Entidade {id} na posição {grid.Positions[id]} está fora da caixa {box}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-SPATIAL-03] Busca por AABB (QueryBox) retornando somente entidades na caixa: PASS");
        }

        /// <summary>
        /// TEST-SPATIAL-04: Zero-GC em 10.000 queries consecutivas (0 bytes alocados no Heap).
        /// </summary>
        private static void Test_TEST_SPATIAL_04(ref int passCount, ref int totalCount, out long deltaBytes)
        {
            totalCount++;
            const int unitCount = 1000;
            var grid = new SpatialHashGrid(2048, 2048, 32.0f, 2048);

            var rnd = new Random(42);
            for (int i = 0; i < unitCount; i++)
            {
                grid.Insert(i, new Vector2((float)rnd.Next(50, 1950), (float)rnd.Next(50, 1950)));
            }

            int[] preallocatedBuffer = new int[64];
            Span<int> bufferSpan = preallocatedBuffer.AsSpan();
            var queryBox = new RectangleF(500f, 500f, 300f, 300f);

            // Aquecimento
            for (int i = 0; i < 100; i++)
            {
                grid.FindNeighbors(new Vector2(600f, 600f), 50f, bufferSpan);
                grid.QueryBox(queryBox, bufferSpan);
            }

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 10000; i++)
            {
                Vector2 center = new Vector2(500f + (i % 200), 500f + ((i * 7) % 200));
                grid.FindNeighbors(center, 40f, bufferSpan);
                grid.QueryBox(queryBox, bufferSpan);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            deltaBytes = bytesAfter - bytesBefore;

            if (deltaBytes != 0)
                throw new Exception($"[TEST-SPATIAL-04] Falha Zero-GC: alocou {deltaBytes} bytes durante 10.000 queries espaciais!");

            passCount++;
            Console.WriteLine("  -> [TEST-SPATIAL-04] Zero-GC em 10.000 queries consecutivas (0 bytes no Heap): PASS");
        }
    }
}
