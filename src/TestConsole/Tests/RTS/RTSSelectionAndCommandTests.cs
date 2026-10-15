using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Resolution;
using MonoGameMaker.Runtime.Selection;
using MonoGameMaker.Runtime.Spatial;

namespace TestConsole.Tests.RTS
{
    public static class RTSSelectionAndCommandTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE RTS 5/6] Executando RTSSelectionAndCommandTests (TEST-SEL-01 a 04)...");

            Test_TEST_SEL_01(ref passCount, ref totalCount);
            Test_TEST_SEL_02(ref passCount, ref totalCount);
            Test_TEST_SEL_03(ref passCount, ref totalCount);
            Test_TEST_SEL_04(ref passCount, ref totalCount);
        }

        /// <summary>
        /// TEST-SEL-01: Marquee Box selecionando unidades dentro da caixa convertida via ResolutionManager e Câmera.
        /// </summary>
        private static void Test_TEST_SEL_01(ref int passCount, ref int totalCount)
        {
            totalCount++;

            // Configurar resolução física 1440x1080 (proporção 4:3 -> Letterbox ativo com OffsetY = 135px e Scale = 0.75f)
            ResolutionManager.SetResolution(1440, 1080);

            // Câmera com Zoom 2.0x centralizada na origem
            Matrix cameraTransform = Matrix.CreateScale(2.0f);

            var spatialGrid = new SpatialHashGrid(2048, 2048, 32.0f, 64);
            var selectionManager = new RTSSelectionManager(spatialGrid, 64);

            byte playerFaction = 1;
            byte[] factions = new byte[64];
            var roles = new UnitTacticalRole[64];
            Array.Fill(factions, playerFaction);
            Array.Fill(roles, UnitTacticalRole.Combat);

            // Coordenadas de tela física: de (360, 270) a (1080, 810)
            Vector2 screenStart = new Vector2(360f, 270f);
            Vector2 screenEnd   = new Vector2(1080f, 810f);

            // Calcular o retângulo de mundo esperado matematicamente:
            // ScreenToVirtual: vx = (360 - 0)/0.75 = 480; vy = (270 - 135)/0.75 = 180
            //                  vx2 = (1080 - 0)/0.75 = 1440; vy2 = (810 - 135)/0.75 = 900
            // CamInvert (Zoom 2.0x -> divide por 2):
            //                  wx1 = 240; wy1 = 90
            //                  wx2 = 720; wy2 = 450
            var expectedWorldBox = new RectangleF(240f, 90f, 480f, 360f);

            // Povoar 10 unidades: 5 dentro da caixa projetada, 5 fora
            Vector2[] unitPositions = new Vector2[10];
            // Dentro:
            unitPositions[0] = new Vector2(300f, 150f);
            unitPositions[1] = new Vector2(400f, 200f);
            unitPositions[2] = new Vector2(500f, 300f);
            unitPositions[3] = new Vector2(600f, 400f);
            unitPositions[4] = new Vector2(700f, 420f);
            // Fora:
            unitPositions[5] = new Vector2(100f, 100f); // wx < 240
            unitPositions[6] = new Vector2(800f, 200f); // wx > 720
            unitPositions[7] = new Vector2(400f, 50f);  // wy < 90
            unitPositions[8] = new Vector2(500f, 500f); // wy > 450
            unitPositions[9] = new Vector2(50f, 50f);

            for (int i = 0; i < 10; i++)
            {
                spatialGrid.Insert(i, unitPositions[i]);
            }

            selectionManager.SelectBox(
                screenStart,
                screenEnd,
                cameraTransform,
                factions,
                roles,
                playerFaction,
                isAdditive: false);

            if (selectionManager.SelectedCount != 5)
                throw new Exception($"[TEST-SEL-01] Esperado 5 unidades selecionadas na caixa convertida, obtido {selectionManager.SelectedCount}");

            var selected = selectionManager.SelectedEntities;
            for (int i = 0; i < selected.Length; i++)
            {
                int id = selected[i];
                if (!expectedWorldBox.Contains(unitPositions[id]))
                    throw new Exception($"[TEST-SEL-01] Unidade {id} na posição {unitPositions[id]} não está contida na caixa {expectedWorldBox}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-SEL-01] Marquee Box selecionando unidades dentro da caixa convertida: PASS");
        }

        /// <summary>
        /// TEST-SEL-02: Prioridade militar: seleciona combatentes e descarta operários se ambos estiverem na caixa.
        /// </summary>
        private static void Test_TEST_SEL_02(ref int passCount, ref int totalCount)
        {
            totalCount++;

            ResolutionManager.SetResolution(1920, 1080);
            var spatialGrid = new SpatialHashGrid(1024, 1024, 32.0f, 64);
            var selectionManager = new RTSSelectionManager(spatialGrid, 64);

            byte playerFaction = 1;
            byte[] factions = new byte[64];
            var roles = new UnitTacticalRole[64];
            Array.Fill(factions, playerFaction);

            // 10 combatentes (ids 0..9) e 5 trabalhadores civis (ids 10..14)
            for (int i = 0; i < 10; i++)
            {
                roles[i] = UnitTacticalRole.Combat;
                spatialGrid.Insert(i, new Vector2(200f + (i * 10f), 200f));
            }

            for (int i = 10; i < 15; i++)
            {
                roles[i] = UnitTacticalRole.Worker;
                spatialGrid.Insert(i, new Vector2(200f + ((i - 10) * 10f), 250f));
            }

            // Selecionar área que abrange ambos
            Vector2 screenStart = new Vector2(150f, 150f);
            Vector2 screenEnd   = new Vector2(400f, 300f);

            selectionManager.SelectBox(
                screenStart,
                screenEnd,
                Matrix.Identity,
                factions,
                roles,
                playerFaction,
                isAdditive: false);

            // Deve conter EXATAMENTE 10 unidades e NENHUM operário
            if (selectionManager.SelectedCount != 10)
                throw new Exception($"[TEST-SEL-02] Prioridade militar falhou: esperado 10 combatentes, obtido {selectionManager.SelectedCount}");

            foreach (int id in selectionManager.SelectedEntities)
            {
                if (roles[id] == UnitTacticalRole.Worker)
                    throw new Exception($"[TEST-SEL-02] Operário {id} foi selecionado indevidamente em presença de combatentes!");
            }

            // Caso B: Apenas operários dentro da caixa
            Vector2 screenStartWorkers = new Vector2(150f, 240f);
            Vector2 screenEndWorkers   = new Vector2(400f, 270f);

            selectionManager.SelectBox(
                screenStartWorkers,
                screenEndWorkers,
                Matrix.Identity,
                factions,
                roles,
                playerFaction,
                isAdditive: false);

            if (selectionManager.SelectedCount != 5)
                throw new Exception($"[TEST-SEL-02] Seleção exclusiva de operários falhou: esperado 5, obtido {selectionManager.SelectedCount}");

            foreach (int id in selectionManager.SelectedEntities)
            {
                if (roles[id] != UnitTacticalRole.Worker)
                    throw new Exception($"[TEST-SEL-02] Entidade não-operário {id} selecionada em caixa exclusiva de operários!");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-SEL-02] Prioridade militar (combate vs operários): PASS");
        }

        /// <summary>
        /// TEST-SEL-03: Grupos de controle tático (Ctrl+1..9): armazenamento e recuperação.
        /// </summary>
        private static void Test_TEST_SEL_03(ref int passCount, ref int totalCount)
        {
            totalCount++;

            var cgm = new ControlGroupManager(32);
            int[] initialUnits = { 10, 11, 12, 13, 14, 15, 16, 17 }; // 8 unidades
            cgm.AssignGroup(1, initialUnits);

            // Posições das unidades para centróide
            Vector2[] positions = new Vector2[30];
            for (int i = 0; i < initialUnits.Length; i++)
            {
                positions[initialUnits[i]] = new Vector2(100f + (i * 10f), 200f);
            }

            // Marcar 3 unidades (11, 14, 16) como destruídas
            bool[] isAlive = new bool[30];
            Array.Fill(isAlive, true);
            isAlive[11] = false;
            isAlive[14] = false;
            isAlive[16] = false;

            // Primeiro toque na tecla 1 em t = 1.0s
            bool success = cgm.SelectGroup(
                1,
                1.0f,
                id => isAlive[id],
                out ReadOnlySpan<int> selectedSpan,
                out bool doubleTapJump,
                positions,
                out Vector2 centroid);

            if (!success)
                throw new Exception("[TEST-SEL-03] Falha ao selecionar grupo de controle 1");

            if (selectedSpan.Length != 5)
                throw new Exception($"[TEST-SEL-03] Esperado 5 unidades sobreviventes no grupo, obtido {selectedSpan.Length}");

            if (doubleTapJump)
                throw new Exception("[TEST-SEL-03] Primeiro toque não deve ativar doubleTapJump!");

            // Verificar que unidades mortas não estão presentes
            foreach (int id in selectedSpan)
            {
                if (!isAlive[id])
                    throw new Exception($"[TEST-SEL-03] Unidade destruída {id} ainda consta no grupo de controle!");
            }

            // Segundo toque rápido na tecla 1 em t = 1.10s (delta = 0.10s <= 0.25s)
            success = cgm.SelectGroup(
                1,
                1.10f,
                id => isAlive[id],
                out selectedSpan,
                out doubleTapJump,
                positions,
                out centroid);

            if (!success || !doubleTapJump)
                throw new Exception("[TEST-SEL-03] Double-tap jump deveria ser true com intervalo de 0.10s!");

            // Centróide esperado: média das 5 unidades sobreviventes (10, 12, 13, 15, 17)
            Vector2 expectedCentroid = (positions[10] + positions[12] + positions[13] + positions[15] + positions[17]) / 5f;
            if (!RtsTestMath.ApproximatelyEqual(centroid, expectedCentroid, 1e-4f))
            {
                throw new Exception($"[TEST-SEL-03] Centróide incorreto: esperado {expectedCentroid}, obtido {centroid}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-SEL-03] Grupos de controle tático (Ctrl+1..9) e salto de câmera: PASS");
        }

        /// <summary>
        /// TEST-SEL-04: Fila de comandos e despacho de marcha.
        /// </summary>
        private static void Test_TEST_SEL_04(ref int passCount, ref int totalCount)
        {
            totalCount++;

            var queue = new UnitCommandQueue(16);

            Vector2 targetA = new Vector2(100f, 100f);
            Vector2 targetB = new Vector2(250f, 300f);

            // 1. Enviar comando Move(A) sem Shift: substitui a fila
            queue.Enqueue(new UnitCommand(UnitCommandType.Move, targetA), queueOrder: false);
            if (queue.Count != 1)
                throw new Exception($"[TEST-SEL-04] Esperado 1 comando na fila, obtido {queue.Count}");

            // 2. Enviar comandos com Shift (queueOrder: true): Move(B) e HoldPosition
            queue.Enqueue(new UnitCommand(UnitCommandType.Move, targetB), queueOrder: true);
            queue.Enqueue(new UnitCommand(UnitCommandType.HoldPosition, Vector2.Zero), queueOrder: true);

            if (queue.Count != 3)
                throw new Exception($"[TEST-SEL-04] Esperado 3 comandos encadeados, obtido {queue.Count}");

            // 3. Consome primeiro comando (Move A)
            if (!queue.TryDequeue(out var cmd1) || cmd1.Type != UnitCommandType.Move || cmd1.TargetPosition != targetA)
                throw new Exception("[TEST-SEL-04] Primeiro comando desenfileirado incorreto!");

            // 4. Chegada a A: consome próximo comando (Move B)
            if (!queue.TryDequeue(out var cmd2) || cmd2.Type != UnitCommandType.Move || cmd2.TargetPosition != targetB)
                throw new Exception("[TEST-SEL-04] Segundo comando desenfileirado incorreto!");

            // 5. Consome HoldPosition
            if (!queue.TryDequeue(out var cmd3) || cmd3.Type != UnitCommandType.HoldPosition)
                throw new Exception("[TEST-SEL-04] Terceiro comando desenfileirado incorreto!");

            if (!queue.IsEmpty)
                throw new Exception("[TEST-SEL-04] A fila deveria estar vazia após consumir todas as ordens!");

            // 6. Novo comando sem Shift limpa imediatamente
            queue.Enqueue(new UnitCommand(UnitCommandType.Move, targetA), queueOrder: true);
            queue.Enqueue(new UnitCommand(UnitCommandType.Stop, Vector2.Zero), queueOrder: false);
            if (queue.Count != 1)
                throw new Exception($"[TEST-SEL-04] Comando sem shift deveria ter resetado a fila para 1, obtido {queue.Count}");

            passCount++;
            Console.WriteLine("  -> [TEST-SEL-04] Fila de comandos sequencial e despacho de marcha: PASS");
        }
    }
}
