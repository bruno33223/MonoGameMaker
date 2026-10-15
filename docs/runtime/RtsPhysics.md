# `RtsPhysics` Manual da API

A classe estática `RtsPhysics` encapsula os núcleos matemáticos puros para dinâmica de multidões e relaxamento de colisões no RTS do MonoGameMaker. Ela implementa a força suave de repulsão de Reynolds com amortecimento de singularidade ($\propto 1/(d^2 + \epsilon)$) e o algoritmo rígido de projeção posicional mútua (Circle-Circle Hard Push Constraint), impedindo que unidades militares atravessem umas às outras sob qualquer densidade de agrupamento.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Simulation
{
    public static class RtsPhysics
    {
        // Constante anti-singularidade para evitar divisão por zero quando entidades se sobrepõem exatamente
        public const float EpsilonAntiSingularity = 0.001f;

        /// <summary>
        /// Calcula a força suave de repulsão de Reynolds: F = dir * (weight / (d^2 + epsilon)).
        /// </summary>
        public static Vector2 CalculateReynoldsRepulsion(
            Vector2 posA,
            Vector2 posB,
            float weight = 45.0f,
            float epsilon = EpsilonAntiSingularity);

        /// <summary>
        /// Aplica o relaxamento posicional direto (Circle-Circle Hard Push Constraint).
        /// Garante geometricamente que dist(posA, posB) >= radiusA + radiusB com jitter determinístico para dist == 0.
        /// Retorna true se houve penetração e correção posicional.
        /// </summary>
        public static bool ResolveHardPush(
            ref Vector2 posA,
            ref Vector2 posB,
            float radiusA,
            float radiusB,
            int idA = 0,
            int idB = 1);

        /// <summary>
        /// Executa relaxamento rígido iterativo em lote sobre spans contíguos de posições e raios.
        /// </summary>
        public static void RelaxHardPushBatch(
            Span<Vector2> positions,
            ReadOnlySpan<float> radii,
            int iterations = 10);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Flocking Suave e Desvio Natural (Steering Behaviors):**
   Ao calcular as forças de direção de um soldado em marcha, consulte seus vizinhos imediatos e some as contribuições de `CalculateReynoldsRepulsion(myPos, neighborPos)`. Isso produz curvas orgânicas de afastamento sem desaceleração abrupta.
2. **Garantia Inegociável de Não-Penetração (Hard Push):**
   Forças de repulsão baseadas apenas em aceleração falham quando centenas de tropas são pressionadas contra passagens estreitas (chokepoints) ou quando convergem para o mesmo alvo. Nesses casos, `ResolveHardPush` atua no nível geométrico pós-integração de Euler, projetando as coordenadas no espaço para anular a sobreposição instantaneamente.
3. **Desempate Determinístico de Coincidência Exata ($d = 0$):**
   Se duas unidades nascem exatamente na mesma coordenada $(X, Y)$, `ResolveHardPush` utiliza uma dispersão pseudo-ortogonal determinística baseada nos IDs das entidades (`idA`, `idB`), eliminando NaN ou vetores nulos sem depender de alocações aleatórias.
4. **Relaxamento de Cadeias Físicas em Grupo (`RelaxHardPushBatch`):**
   Útil para simulações locais ou resolução rápida de aglomerados densos desacoplados do `RtsSimulationSystem`.

---

## Exemplo Prático e Compilável

O exemplo abaixo demonstra como utilizar os métodos estáticos de `RtsPhysics` dentro de um ciclo de colisão e empurrão entre dois soldados no `Update`:

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Simulation;

public class BumpingUnitsController
{
    private Vector2 _soldierAPos = new Vector2(100f, 100f);
    private Vector2 _soldierBPos = new Vector2(105f, 100f);
    private const float UnitRadius = 10f; // Distância mínima desejada = 20f

    public void Update(GameTime gameTime)
    {
        // 1. Calcula vetor de repulsão suave de Reynolds
        Vector2 smoothRepulsion = RtsPhysics.CalculateReynoldsRepulsion(
            _soldierAPos, 
            _soldierBPos, 
            weight: 50f);

        // Aplica afastamento cinemático suave
        _soldierAPos += smoothRepulsion * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 2. Garante restrição rígida inegociável (Hard Push Constraint)
        bool wasOverlapping = RtsPhysics.ResolveHardPush(
            ref _soldierAPos, 
            ref _soldierBPos, 
            radiusA: UnitRadius, 
            radiusB: UnitRadius, 
            idA: 1, 
            idB: 2);

        if (wasOverlapping)
        {
            // Ambas as posições foram afastadas simetricamente para que dist >= 20f
        }
    }
}
```
