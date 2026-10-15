# `RTSSelectionManager` Manual da API

O `RTSSelectionManager` (e seu gestor de atalhos `ControlGroupManager`) é o centro de controle tático do módulo RTS do MonoGameMaker. Integrado matematicamente com o `ResolutionManager` (compensando Letterbox/Pillarbox) e a matriz inversa da `Camera2D`, ele provê seleção precisa por clique único e caixa elástica (*Marquee Box*), priorização automática de tropas militares sobre trabalhadores civis, comandos com fila de ordens (`UnitCommand`) e grupos de controle tático (`Ctrl+1..9`) com salto suave de câmera (*Double-Tap* em $<0.25\text{s}$).

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Selection
{
    public enum UnitTacticalRole : byte
    {
        Combat = 0,    // Soldado, Tanque, Aeronave
        Worker = 1     // Colhedor, Construtor, Civil
    }

    public enum UnitCommandType : byte
    {
        Move,
        AttackMove,
        HoldPosition,
        Stop,
        AttackTarget
    }

    public readonly struct UnitCommand
    {
        public UnitCommandType Type { get; }
        public Vector2 TargetPosition { get; }
        public int TargetEntityId { get; }

        public UnitCommand(UnitCommandType type, Vector2 targetPos, int targetEntityId = -1);
    }

    public sealed class RTSSelectionManager
    {
        public ReadOnlySpan<int> SelectedEntities { get; }
        public int SelectedCount { get; }

        public RTSSelectionManager(SpatialHashGrid spatialGrid, int maxSelection = 512);

        public void ClearSelection();

        public void SelectBox(
            Vector2 screenStart, 
            Vector2 screenEnd, 
            Matrix cameraTransform, 
            ReadOnlySpan<byte> factions, 
            ReadOnlySpan<UnitTacticalRole> roles, 
            byte playerFaction, 
            bool isAdditive);
    }

    public sealed class ControlGroupManager
    {
        public ControlGroupManager(int maxUnitsPerGroup = 256);

        public void AssignGroup(int slot, ReadOnlySpan<int> entities);
        public bool SelectGroup(int slot, float currentTime, out ReadOnlySpan<int> entities, out bool doubleTapJump);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Seleção Imune a Distorções de Resolução e Câmera:**
   Ao arrastar a caixa do mouse na tela, passe diretamente as coordenadas brutas do mouse para `SelectBox(...)`. O gerenciador converte a tela física para o espaço virtual $1920 \times 1080$, compensa o letterbox e aplica a inversa da matriz da câmera, garantindo seleção com precisão de pixel.
2. **Prioridade Militar Automática:**
   Se uma caixa de seleção englobar tanto soldados de combate quanto civis/colhedores, o `RTSSelectionManager` automaticamente descarta os trabalhadores, evitando ordens acidentais de ataque a civis.
3. **Agrupamento com Teclas Numéricas (Ctrl+1..9):**
   Use `ControlGroupManager` para memorizar seleções ativas. Quando o usuário toca duas vezes na mesma tecla em menos de 0.25 segundos, o parâmetro `doubleTapJump` retorna `true`, instruindo a câmera a centralizar no esquadrão.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a captura de arrasto de mouse e seleção tática com o `RTSSelectionManager`:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Selection;
using MonoGameMaker.Runtime.Spatial;

public class TacticalInputHandler
{
    private readonly RTSSelectionManager _selectionManager;
    private readonly ControlGroupManager _groupManager;
    private readonly byte[] _factions;
    private readonly UnitTacticalRole[] _roles;
    private Vector2? _dragStart;

    public TacticalInputHandler(SpatialHashGrid spatialGrid, int unitCapacity)
    {
        _selectionManager = new RTSSelectionManager(spatialGrid, maxSelection: 256);
        _groupManager = new ControlGroupManager();

        _factions = new byte[unitCapacity];
        _roles = new UnitTacticalRole[unitCapacity];
    }

    public void HandleInput(MouseState mouse, KeyboardState keyboard, Matrix cameraTransform, float totalGameTime)
    {
        Vector2 mousePos = new Vector2(mouse.X, mouse.Y);

        // 1. Início do arrasto da caixa de seleção (botão esquerdo)
        if (mouse.LeftButton == ButtonState.Pressed && !_dragStart.HasValue)
        {
            _dragStart = mousePos;
        }
        // 2. Final do arrasto
        else if (mouse.LeftButton == ButtonState.Released && _dragStart.HasValue)
        {
            Vector2 dragEnd = mousePos;
            bool isShiftHeld = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

            _selectionManager.SelectBox(
                _dragStart.Value,
                dragEnd,
                cameraTransform,
                _factions.AsSpan(),
                _roles.AsSpan(),
                playerFaction: 0,
                isAdditive: isShiftHeld
            );

            _dragStart = null;
        }

        // 3. Atalhos de Grupo de Controle (Ctrl + 1 para gravar, 1 para selecionar)
        if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.D1))
        {
            _groupManager.AssignGroup(1, _selectionManager.SelectedEntities);
        }
        else if (keyboard.IsKeyDown(Keys.D1))
        {
            if (_groupManager.SelectGroup(1, totalGameTime, out var groupEntities, out bool jump))
            {
                // Seleciona o grupo recuperado
            }
        }
    }
}
```
