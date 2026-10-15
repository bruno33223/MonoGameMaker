# `ControlGroupManager` Manual da API

O `ControlGroupManager` é o coordenador de esquadrões e agrupamentos táticos do módulo RTS do MonoGameMaker. Ele implementa o mapeamento clássico de atalhos de teclado `Ctrl+1..9` (criação de grupos) e `Shift+1..9` (adição cumulativa sem duplicatas), com suporte nativo a descarte automático de unidades mortas e detecção de duplo toque (*double-tap* em $<0.25\text{s}$) para salto de câmera.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Selection
{
    public sealed class ControlGroupManager
    {
        public ControlGroupManager(int maxUnitsPerGroup = 256);

        public void AssignGroup(int slot, ReadOnlySpan<int> entities);
        public void AddToGroup(int slot, ReadOnlySpan<int> entities);

        public bool SelectGroup(int slot, float currentTime, out ReadOnlySpan<int> entities, out bool doubleTapJump);
        public bool SelectGroup(int slot, float currentTime, Func<int, bool>? isAlive, out ReadOnlySpan<int> entities, out bool doubleTapJump);

        public void PruneDeadInSlot(int slot, Func<int, bool> isAlive);
        public void PruneDead(Func<int, bool> isAlive);

        public static Vector2 CalculateCentroid(ReadOnlySpan<int> entities, ReadOnlySpan<Vector2> positions);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Gestão de Grupos de Combate Rápidos:**
   Permite que o jogador vincule seleções ativas a teclas numéricas (ex: Grupo 1 = Tanques, Grupo 2 = Artilharia).
2. **Adição Incremental (Shift):**
   Ao recrutar novas unidades na base, selecione-as e pressione `Shift+1` para mesclá-las ao grupo existente sem sobrescrever a formação prévia.
3. **Limpeza Automática de Baixas em Combate:**
   Ao chamar `SelectGroup(slot, time, id => unitAlive[id], out span, out jump)`, qualquer unidade destruída em combate é purgada do slot instantaneamente, evitando referências fantasmas.
4. **Recentralização Dinâmica de Câmera:**
   Quando `doubleTapJump == true`, calcule o ponto médio do esquadrão via `CalculateCentroid(...)` e anime a câmera para o centro da batalha.

---

## Exemplo Prático e Compilável

```csharp
using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Selection;

public class TacticalSquadSystem
{
    private readonly ControlGroupManager _groupManager = new(maxUnitsPerGroup: 128);
    private readonly bool[] _aliveUnits = new bool[1024];
    private readonly Vector2[] _unitPositions = new Vector2[1024];

    public void OnGroupKey(int slot, float gameTime, bool ctrlHeld, bool shiftHeld, ReadOnlySpan<int> currentSelection)
    {
        if (ctrlHeld)
        {
            _groupManager.AssignGroup(slot, currentSelection);
        }
        else if (shiftHeld)
        {
            _groupManager.AddToGroup(slot, currentSelection);
        }
        else
        {
            if (_groupManager.SelectGroup(slot, gameTime, id => _aliveUnits[id], out var units, out bool doubleTap))
            {
                if (doubleTap)
                {
                    Vector2 center = ControlGroupManager.CalculateCentroid(units, _unitPositions);
                    // Centralizar a câmera em center
                }
            }
        }
    }
}
```
