# `RtsSimulationSystem` Manual da API

O `RtsSimulationSystem` é o orquestrador central e de altíssimo desempenho do MonoGameMaker para simulação militar em massa no paradigma Data-Oriented Design (DoD). Ele unifica o particionamento espacial em grade CSR (`SpatialHashGrid`), navegação por vetores de gradiente (`FlowField`), repulsão suave de Reynolds, integração de Euler semi-implícito com arrasto e relaxamento iterativo de penetração rígida (Hard Push) com garantia estrita de **Zero-GC** (0 bytes alocados no Heap durante todo o loop de execução).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Simulation
{
    public sealed class RtsSimulationSystem
    {
        public int MaxUnits { get; }
        public SpatialHashGrid SpatialGrid { get; }
        public ReadOnlySpan<RtsUnitPhysicsData> Units { get; }

        public RtsSimulationSystem(
            int maxUnits, 
            int worldWidth, 
            int worldHeight, 
            float cellSize = 32f);

        public void SetUnit(int id, in RtsUnitPhysicsData data);
        public ref RtsUnitPhysicsData GetUnitRef(int id);
        public Vector2 GetUnitPosition(int id);

        public void Update(
            float deltaTime, 
            MonoGameMaker.Runtime.Pathfinding.FlowField? activeFlowField = null);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Simulação de 1.000 Unidades a 60 FPS:**
   Evite a todo custo criar 1.000 instâncias de `GameEntity` com scripts individuais e `Update()` dinâmico. O `RtsSimulationSystem` processa todo o contingente militar em buffers contíguos de memória, garantindo cache hit na CPU e livrando a engine de pausas por coleta de lixo.
2. **Navegação de Enxame via Flow Field:**
   Ao emitir ordens de marcha em massa, passe o `FlowField` gerado para o método `Update(dt, flowField)`. O sistema automaticamente calcula a velocidade desejada de cada unidade por amostragem bilinear, combina com as forças locais de separação e limita pela aceleração máxima da tropa.
3. **Resolução de Micro-Colisões em Cadeia:**
   Durante o passo 3 do ciclo de atualização, o sistema executa até 12 sub-iterações de relaxamento com reindexação dinâmica na grade espacial, dispersando até mesmo aglomerações extremas de tropas em gargalos sem permitir sobreposição visual.
4. **Alimentação do Renderizador Instanciado:**
   Acesse a propriedade `Units` (um `ReadOnlySpan<RtsUnitPhysicsData>`) para copiar diretamente as posições e rotações para o buffer de vértices do `InstancedUnitRenderer`.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra a instanciação, configuração de tropas e ciclo de atualização completo integrado com MonoGame:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Pathfinding;
using MonoGameMaker.Runtime.Simulation;

public class BattleSimulationScene
{
    private readonly RtsSimulationSystem _simulation;
    private readonly FlowFieldManager _flowManager;
    private FlowField? _activeFlowField;

    public BattleSimulationScene()
    {
        // Cria o mundo de simulação de 2048x2048 para até 1000 tropas com células de 32px
        _simulation = new RtsSimulationSystem(maxUnits: 1000, worldWidth: 2048, worldHeight: 2048, cellSize: 32f);
        _flowManager = new FlowFieldManager(worldWidth: 2048, worldHeight: 2048, cellSize: 32f);

        // Inicializa 500 combatentes em formação
        for (int i = 0; i < 500; i++)
        {
            var unit = new RtsUnitPhysicsData
            {
                Position = new Vector2(100f + (i % 25) * 22f, 100f + (i / 25) * 22f),
                Velocity = Vector2.Zero,
                Rotation = 0f,
                Radius = 10f,
                MaxSpeed = 100f,
                MaxForce = 300f,
                Mass = 1.0f,
                TurnSpeed = 10f,
                FlowFieldId = 0,
                IsActive = 1,
                TeamId = 1
            };

            _simulation.SetUnit(i, in unit);
        }
    }

    public void OrderArmyTo(Vector2 targetWorldPos)
    {
        // Requisita ou calcula campo de fluxo para o destino
        _activeFlowField = _flowManager.GetOrCreateFlowField(targetWorldPos);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0f) return;

        // Atualiza toda a simulação física e espacial em uma única chamada Zero-GC
        _simulation.Update(dt, _activeFlowField);
    }
}
```
