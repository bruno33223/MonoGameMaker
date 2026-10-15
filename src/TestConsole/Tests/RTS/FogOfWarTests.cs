using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Vision;

namespace TestConsole.Tests.RTS
{
    public static class FogOfWarTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE RTS 4/6] Executando FogOfWarTests (TEST-FOG-01 a 03)...");

            Test_TEST_FOG_01(ref passCount, ref totalCount);
            Test_TEST_FOG_02(ref passCount, ref totalCount);
            Test_TEST_FOG_03(ref passCount, ref totalCount);
        }

        /// <summary>
        /// TEST-FOG-01: Transição de estado: Unexplored (0) -> Visible (255) -> Explored (128).
        /// </summary>
        private static void Test_TEST_FOG_01(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int gridW = 64;
            int gridH = 64;
            float cellSize = 16.0f;

            using var fog = new FogOfWarSystem(null, gridW, gridH, cellSize);

            Vector2 center = new Vector2(100f, 100f);
            float radius = 50.0f;

            // 1. Inicial: todas as células devem ser Unexplored (0)
            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    Vector2 pos = new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                    if (fog.GetVisibilityAt(pos) != FogState.Unexplored)
                        throw new Exception($"[TEST-FOG-01] Estado inicial não é Unexplored em {pos}");
                }
            }

            // 2. Passe 1: Revela o círculo
            fog.BeginVisionPass();
            fog.RevealCircle(center, radius);
            fog.EndVisionPass();

            if (fog.GetVisibilityAt(center) != FogState.Visible)
                throw new Exception($"[TEST-FOG-01] Célula central ({center}) deveria estar Visible após RevealCircle!");

            // 3. Passe 2: Sem revelar a área (unidade saiu do alcance)
            fog.BeginVisionPass();
            fog.EndVisionPass();

            // A célula central agora deve estar Explored (128)
            FogState stateAfter = fog.GetVisibilityAt(center);
            if (stateAfter != FogState.Explored)
                throw new Exception($"[TEST-FOG-01] Célula central deveria transitar para Explored (128), obtido {stateAfter} ({(byte)stateAfter})");

            // Células distantes (ex: 800, 800) nunca visitadas continuam Unexplored (0)
            Vector2 farPos = new Vector2(800f, 800f);
            if (fog.GetVisibilityAt(farPos) != FogState.Unexplored)
                throw new Exception($"[TEST-FOG-01] Célula distante {farPos} deveria continuar Unexplored (0), obtido {fog.GetVisibilityAt(farPos)}");

            passCount++;
            Console.WriteLine("  -> [TEST-FOG-01] Transição de estado: Unexplored (0) -> Visible (255) -> Explored (128): PASS");
        }

        /// <summary>
        /// TEST-FOG-02: Projeção circular exata com raio de visão delimitado (LoS Raster).
        /// </summary>
        private static void Test_TEST_FOG_02(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int gridW = 32;
            int gridH = 32;
            float cellSize = 16.0f;

            using var fog = new FogOfWarSystem(null, gridW, gridH, cellSize);

            Vector2 center = new Vector2(64f, 64f); // Célula (4, 4)
            float radius = 48.0f;                   // Exatamente 3 células de raio

            fog.BeginVisionPass();
            fog.RevealCircle(center, radius);
            fog.EndVisionPass();

            int cx = (int)(center.X / cellSize);
            int cy = (int)(center.Y / cellSize);
            int rCells = (int)MathF.Ceiling(radius / cellSize);
            int rSq = rCells * rCells;

            // Varrer toda a grade
            for (int y = 0; y < gridH; y++)
            {
                int dy = y - cy;
                int rowOffset = y * gridW;
                for (int x = 0; x < gridW; x++)
                {
                    int dx = x - cx;
                    int distSq = dx * dx + dy * dy;
                    byte vis = fog.VisibleBuffer[rowOffset + x];

                    if (distSq <= rSq)
                    {
                        if (vis != 255)
                            throw new Exception($"[TEST-FOG-02] Célula ({x}, {y}) a distSq={distSq} <= {rSq} deveria ser visível (255), obtido {vis}");
                    }
                    else
                    {
                        if (vis != 0)
                            throw new Exception($"[TEST-FOG-02] Célula ({x}, {y}) fora do raio (distSq={distSq} > {rSq}) foi marcada indevidamente com {vis}");
                    }
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-FOG-02] Projeção circular exata com raio de visão delimitado: PASS");
        }

        /// <summary>
        /// TEST-FOG-03: Culling de entidades: entidades em Unexplored ou Explored não visíveis.
        /// </summary>
        private static void Test_TEST_FOG_03(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int gridW = 64;
            int gridH = 64;
            float cellSize = 16.0f;

            using var fog = new FogOfWarSystem(null, gridW, gridH, cellSize);

            byte localTeam = 1;
            byte enemyTeam = 2;

            Vector2 posExplored = new Vector2(100f, 100f);
            Vector2 posVisible  = new Vector2(300f, 300f);
            Vector2 posUnexp    = new Vector2(800f, 800f);

            // Passe 1: Revela posExplored
            fog.BeginVisionPass();
            fog.RevealCircle(posExplored, 40f);
            fog.EndVisionPass();

            // Passe 2: Revela posVisible (fazendo posExplored decair para Explored)
            fog.BeginVisionPass();
            fog.RevealCircle(posVisible, 40f);
            fog.EndVisionPass();

            // Verificar estados de névoa
            if (fog.GetVisibilityAt(posUnexp) != FogState.Unexplored)
                throw new Exception("[TEST-FOG-03] Configuração de teste: posUnexp deveria ser Unexplored");

            if (fog.GetVisibilityAt(posExplored) != FogState.Explored)
                throw new Exception("[TEST-FOG-03] Configuração de teste: posExplored deveria ser Explored");

            if (fog.GetVisibilityAt(posVisible) != FogState.Visible)
                throw new Exception("[TEST-FOG-03] Configuração de teste: posVisible deveria ser Visible");

            // Culling de entidades inimigas:
            // Entidade Inimiga em Unexplored -> Oculta
            if (fog.IsEntityVisible(posUnexp, enemyTeam, localTeam))
                throw new Exception("[TEST-FOG-03] Inimigo em Unexplored não pode ser visível!");

            // Entidade Inimiga em Explored -> Oculta
            if (fog.IsEntityVisible(posExplored, enemyTeam, localTeam))
                throw new Exception("[TEST-FOG-03] Inimigo em Explored não pode ser visível!");

            // Entidade Inimiga em Visible -> Visível
            if (!fog.IsEntityVisible(posVisible, enemyTeam, localTeam))
                throw new Exception("[TEST-FOG-03] Inimigo em área Visible deve ser visível!");

            // Entidade Aliada em Unexplored -> Sempre visível para o jogador
            if (!fog.IsEntityVisible(posUnexp, localTeam, localTeam))
                throw new Exception("[TEST-FOG-03] Aliado deve ser visível independentemente da névoa de guerra!");

            passCount++;
            Console.WriteLine("  -> [TEST-FOG-03] Culling de entidades em Unexplored ou Explored: PASS");
        }
    }
}
