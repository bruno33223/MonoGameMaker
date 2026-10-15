using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;

namespace TestConsole.Tests.RTS
{
    public static class FlowFieldPathfindingTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE RTS 2/6] Executando FlowFieldPathfindingTests (TEST-FLOW-01 a 05)...");

            Test_TEST_FLOW_01(ref passCount, ref totalCount);
            Test_TEST_FLOW_02(ref passCount, ref totalCount);
            Test_TEST_FLOW_03(ref passCount, ref totalCount);
            Test_TEST_FLOW_04(ref passCount, ref totalCount);
            Test_TEST_FLOW_05(ref passCount, ref totalCount);
        }

        /// <summary>
        /// TEST-FLOW-01: CostField com bloqueios intransponíveis (custo 255).
        /// </summary>
        private static void Test_TEST_FLOW_01(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int width = 32;
            int height = 32;
            float cellSize = 32.0f;

            var costField = new CostField(width, height, cellSize);
            var integrationField = new IntegrationField(width, height);
            var flowField = new FlowField(width, height, cellSize);

            // Criar barreira horizontal intransponível na linha y=16 (x de 5 a 25)
            for (int x = 5; x <= 25; x++)
            {
                costField.SetCost(x, 16, CostField.CostBlocked);
            }

            // Destino em (15, 5) (acima da barreira)
            integrationField.Generate(15, 5, costField);
            flowField.Generate(integrationField, costField);

            // Afirmar que todas as células intransponíveis mantêm Integration == ushort.MaxValue e Flow == Vector2.Zero
            for (int x = 5; x <= 25; x++)
            {
                ushort integ = integrationField.GetValue(x, 16);
                if (integ != IntegrationField.Unvisited)
                {
                    throw new Exception($"[TEST-FLOW-01] Célula bloqueada ({x}, 16) possui custo {integ}, esperado Unvisited ({IntegrationField.Unvisited})");
                }

                Vector2 vec = flowField.GetVector(x, 16);
                if (vec != Vector2.Zero)
                {
                    throw new Exception($"[TEST-FLOW-01] Célula bloqueada ({x}, 16) possui vetor {vec}, esperado Vector2.Zero");
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-FLOW-01] CostField com bloqueios intransponíveis (custo 255): PASS");
        }

        /// <summary>
        /// TEST-FLOW-02: IntegrationField gerando gradientes de distância monotônicos até o alvo (custo 0 no destino).
        /// </summary>
        private static void Test_TEST_FLOW_02(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int width = 64;
            int height = 64;
            float cellSize = 32.0f;

            var costField = new CostField(width, height, cellSize);
            var integrationField = new IntegrationField(width, height);

            int goalX = 32;
            int goalY = 32;
            integrationField.Generate(goalX, goalY, costField);

            // 1. Custo no destino é 0
            ushort goalCost = integrationField.GetValue(goalX, goalY);
            if (goalCost != 0)
                throw new Exception($"[TEST-FLOW-02] Destino deve possuir custo 0, obtido {goalCost}");

            // 2. Vizinhos ortogonais imediatos distam 10
            ushort north = integrationField.GetValue(goalX, goalY - 1);
            ushort south = integrationField.GetValue(goalX, goalY + 1);
            ushort west  = integrationField.GetValue(goalX - 1, goalY);
            ushort east  = integrationField.GetValue(goalX + 1, goalY);

            if (north != 10 || south != 10 || west != 10 || east != 10)
                throw new Exception($"[TEST-FLOW-02] Vizinhos ortogonais devem ter custo 10. N={north}, S={south}, W={west}, E={east}");

            // 3. Vizinhos diagonais imediatos distam 14
            ushort nw = integrationField.GetValue(goalX - 1, goalY - 1);
            ushort ne = integrationField.GetValue(goalX + 1, goalY - 1);
            ushort sw = integrationField.GetValue(goalX - 1, goalY + 1);
            ushort se = integrationField.GetValue(goalX + 1, goalY + 1);

            if (nw != 14 || ne != 14 || sw != 14 || se != 14)
                throw new Exception($"[TEST-FLOW-02] Vizinhos diagonais devem ter custo 14. NW={nw}, NE={ne}, SW={sw}, SE={se}");

            // 4. Monotonicidade estrita afastando-se do destino ao longo do eixo cardinal Norte
            ushort prevCost = 0;
            for (int y = goalY - 1; y >= 10; y--)
            {
                ushort cur = integrationField.GetValue(goalX, y);
                if (cur <= prevCost)
                    throw new Exception($"[TEST-FLOW-02] Violação de monotonicidade na coordenada ({goalX}, {y}): custo {cur} <= anterior {prevCost}");
                prevCost = cur;
            }

            passCount++;
            Console.WriteLine("  -> [TEST-FLOW-02] IntegrationField monotônico decrescente até o alvo: PASS");
        }

        /// <summary>
        /// TEST-FLOW-03: FlowField gerando vetores unitários apontando para células de menor integração.
        /// </summary>
        private static void Test_TEST_FLOW_03(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int width = 32;
            int height = 32;
            float cellSize = 32.0f;

            var costField = new CostField(width, height, cellSize);
            var integrationField = new IntegrationField(width, height);
            var flowField = new FlowField(width, height, cellSize);

            int goalX = 10;
            int goalY = 10;
            integrationField.Generate(goalX, goalY, costField);
            flowField.Generate(integrationField, costField);

            // Na célula (10, 20), para chegar em (10, 10), o vetor deve apontar rigorosamente para o Norte (0, -1)
            Vector2 vecNorth = flowField.GetVector(10, 20);
            if (!RtsTestMath.ApproximatelyEqual(vecNorth, new Vector2(0f, -1f), 1e-4f))
                throw new Exception($"[TEST-FLOW-03] Vetor na célula (10, 20) esperado (0, -1), obtido {vecNorth}");

            // Na célula (20, 10), para chegar em (10, 10), o vetor deve apontar rigorosamente para o Oeste (-1, 0)
            Vector2 vecWest = flowField.GetVector(20, 10);
            if (!RtsTestMath.ApproximatelyEqual(vecWest, new Vector2(-1f, 0f), 1e-4f))
                throw new Exception($"[TEST-FLOW-03] Vetor na célula (20, 10) esperado (-1, 0), obtido {vecWest}");

            // No destino (10, 10), o vetor deve ser (0, 0)
            Vector2 vecGoal = flowField.GetVector(goalX, goalY);
            if (vecGoal != Vector2.Zero)
                throw new Exception($"[TEST-FLOW-03] Vetor no destino esperado (0, 0), obtido {vecGoal}");

            // Validação de magnitude unitária (|V| == 1.0) para vetores não nulos
            float magNorth = vecNorth.Length();
            if (MathF.Abs(magNorth - 1.0f) > 1e-5f)
                throw new Exception($"[TEST-FLOW-03] Magnitude do vetor de fluxo deve ser unitária (1.0), obtido {magNorth}");

            passCount++;
            Console.WriteLine("  -> [TEST-FLOW-03] FlowField gerando vetores unitários apontando para menor integração: PASS");
        }

        /// <summary>
        /// TEST-FLOW-04: Amostragem bilinear contínua suave sem travamentos em quinas.
        /// </summary>
        private static void Test_TEST_FLOW_04(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int width = 32;
            int height = 32;
            float cellSize = 32.0f;

            var costField = new CostField(width, height, cellSize);
            var integrationField = new IntegrationField(width, height);
            var flowField = new FlowField(width, height, cellSize);

            integrationField.Generate(5, 5, costField);
            flowField.Generate(integrationField, costField);

            // Amostrar ao longo de uma diagonal contínua com passo fino de 0.5px cruzando limites de células
            Vector2 start = new Vector2(15f * cellSize, 20f * cellSize);
            Vector2 end   = new Vector2(18f * cellSize, 23f * cellSize);
            float totalDist = Vector2.Distance(start, end);
            Vector2 stepDir = Vector2.Normalize(end - start);

            Vector2 prevDir = flowField.SampleSmoothedDirection(start);
            float stepSize = 0.5f;

            for (float d = stepSize; d <= totalDist; d += stepSize)
            {
                Vector2 curPos = start + (stepDir * d);
                Vector2 curDir = flowField.SampleSmoothedDirection(curPos);

                if (curDir != Vector2.Zero && prevDir != Vector2.Zero)
                {
                    float dot = Math.Clamp(Vector2.Dot(prevDir, curDir), -1.0f, 1.0f);
                    float deltaTheta = MathF.Acos(dot);

                    // Em espaço aberto com passo fino de 0.5px, a variação angular não pode exceder 0.15 radianos (~8.5 graus)
                    if (deltaTheta > 0.15f)
                    {
                        throw new Exception($"[TEST-FLOW-04] Descontinuidade angular abrupta detectada em {curPos}: deltaTheta = {deltaTheta:F4} rad (> 0.15 rad)");
                    }
                }
                prevDir = curDir;
            }

            passCount++;
            Console.WriteLine("  -> [TEST-FLOW-04] Amostragem bilinear contínua suave sem descontinuidades: PASS");
        }

        /// <summary>
        /// TEST-FLOW-05: Navegação contornando paredes e obstáculos em U sem mínimos locais.
        /// </summary>
        private static void Test_TEST_FLOW_05(ref int passCount, ref int totalCount)
        {
            totalCount++;
            int width = 40;
            int height = 40;
            float cellSize = 32.0f;

            var costField = new CostField(width, height, cellSize);
            var integrationField = new IntegrationField(width, height);
            var flowField = new FlowField(width, height, cellSize);

            // Construir obstáculo côncavo em "U"
            // Parede esquerda: x=10, y=10..25
            for (int y = 10; y <= 25; y++) costField.SetCost(10, y, CostField.CostBlocked);
            // Fundo do U: y=25, x=10..25
            for (int x = 10; x <= 25; x++) costField.SetCost(x, 25, CostField.CostBlocked);
            // Parede direita: x=25, y=10..25
            for (int y = 10; y <= 25; y++) costField.SetCost(25, y, CostField.CostBlocked);

            // Alvo FORA da concavidade em (17, 2) (ao norte do U)
            int goalX = 17;
            int goalY = 2;
            integrationField.Generate(goalX, goalY, costField);
            flowField.Generate(integrationField, costField);

            // Unidade posicionada no FUNDO do U em (17, 22)
            Vector2 unitPos = new Vector2((17.5f) * cellSize, (22.5f) * cellSize);
            Vector2 goalWorld = new Vector2((goalX + 0.5f) * cellSize, (goalY + 0.5f) * cellSize);

            float speed = 120.0f; // 120 px/s
            float dt = 0.1f;
            bool reachedTarget = false;

            // Simular 350 passos de marcha
            for (int step = 0; step < 350; step++)
            {
                float distToGoal = Vector2.Distance(unitPos, goalWorld);
                if (distToGoal <= cellSize * 2.0f)
                {
                    reachedTarget = true;
                    break;
                }

                Vector2 flow = flowField.SampleSmoothedDirection(unitPos);
                if (flow == Vector2.Zero)
                {
                    throw new Exception($"[TEST-FLOW-05] Unidade presa em mínimo local na coordenada {unitPos} no passo {step}!");
                }

                unitPos += flow * (speed * dt);
            }

            if (!reachedTarget)
                throw new Exception($"[TEST-FLOW-05] Unidade não atingiu o destino fora do obstáculo em U. Posição final: {unitPos}, Alvo: {goalWorld}");

            passCount++;
            Console.WriteLine("  -> [TEST-FLOW-05] Navegação contornando obstáculo em U sem mínimos locais: PASS");
        }
    }
}
