# `RtsUnitPhysicsData` Manual da API

A estrutura `RtsUnitPhysicsData` é a representação contígua, não-gerenciada e compacta de 48 bytes (alinhada à linha de cache da CPU) para o estado cinemático e físico de entidades militares RTS. Ela viabiliza a abordagem Data-Oriented Design (DoD), permitindo que centenas ou milhares de tropas sejam simuladas em arrays lineares sem sobrecarga de objetos no Heap ou fragmentação de memória.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Simulation
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RtsUnitPhysicsData
    {
        public Vector2 Position;       // 8 bytes: Posição global no espaço de simulação
        public Vector2 Velocity;       // 8 bytes: Vetor velocidade linear atual (px/s)
        public float Rotation;         // 4 bytes: Orientação angular (radianos)
        public float Radius;           // 4 bytes: Raio de colisão de corpo rígido
        public float MaxSpeed;         // 4 bytes: Velocidade máxima permitida
        public float MaxForce;         // 4 bytes: Força máxima de aceleração/steering
        public float Mass;             // 4 bytes: Massa inercial (mínimo 0.1f)
        public float TurnSpeed;        // 4 bytes: Taxa angular de rotação (rad/s)
        public int FlowFieldId;        // 4 bytes: Identificador do campo de fluxo ativo
        public byte IsActive;          // 1 byte: Flag de entidade viva (1) ou inativa (0)
        public byte TeamId;            // 1 byte: Identificador da facção/equipe tática
        private ushort _padding;       // 2 bytes de alinhamento para totalizar 48 bytes
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Simulação Massiva Sem Garbage Collection:**
   Ao gerenciar de 500 a 1.000 combatentes simultâneos, alocar instâncias no Heap resulta em *GC spikes* fatais para a taxa de quadros (60 FPS). Utilize `RtsUnitPhysicsData` em buffers estáticos contíguos (`RtsUnitPhysicsData[]`), manipulando referências diretas (`ref RtsUnitPhysicsData`).
2. **Integração com GPU Hardware Instancing:**
   A memória contígua e os dados escalares de `RtsUnitPhysicsData` (`Position`, `Rotation`, `Radius`, `TeamId`) mapeiam diretamente para buffers de instanciamento de vértices (`UnitInstanceData`), minimizando a latência de transferência CPU $\to$ GPU.
3. **Cálculos Vetoriais e Física Euleriana:**
   Fornece os campos físicos estritamente necessários para a integração de Euler semi-implícito com arrasto e resolução de constraints rígidas pelo `RtsSimulationSystem` e `RtsPhysics`.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra como inicializar e atualizar diretamente um contingente de tropas usando `RtsUnitPhysicsData` dentro do `Update` de um jogo MonoGame:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Simulation;

public class RtsTroopManager
{
    private readonly RtsUnitPhysicsData[] _units;
    private const int MaxTroops = 500;

    public RtsTroopManager()
    {
        _units = new RtsUnitPhysicsData[MaxTroops];

        // Inicialização em massa sem alocação dinâmica no Heap
        for (int i = 0; i < MaxTroops; i++)
        {
            _units[i] = new RtsUnitPhysicsData
            {
                Position = new Vector2(100f + (i % 25) * 24f, 100f + (i / 25) * 24f),
                Velocity = Vector2.Zero,
                Rotation = 0f,
                Radius = 10f,
                MaxSpeed = 120f,
                MaxForce = 350f,
                Mass = 1.0f,
                TurnSpeed = 8f,
                FlowFieldId = 0,
                IsActive = 1,
                TeamId = 1
            };
        }
    }

    public void UpdateTroops(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0f) return;

        // Itera por referência para evitar cópia de struct
        for (int i = 0; i < _units.Length; i++)
        {
            ref var unit = ref _units[i];
            if (unit.IsActive == 0) continue;

            // Integração simples de teste para validação de posição
            unit.Position += unit.Velocity * dt;
        }
    }
}
```
