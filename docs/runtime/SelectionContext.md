# SelectionContext

SelectionContext is a core editor component responsible for maintaining the active selection of files (resource paths) and hierarchy nodes (entity instances) in the Mono GameMaker IDE.

## API Signature

```csharp
namespace MonoGameMaker.IDE.Core
{
    public class SelectionContext
    {
        // Event fired when selection changes
        public event Action<object?>? OnSelectionChanged;

        // Current selected entity's primary key (Guid)
        public Guid SelectedEntityId { get; }

        // Current selected resource path relative to project folder
        public string? SelectedResourcePath { get; }

        // Internally update selected entity Guid (usually invoked from SelectNodeCommand)
        public void SetSelectedEntityIdInternal(Guid id);

        // Internally update selected resource path (usually invoked from SelectResourceCommand)
        public void SetSelectedResourcePathInternal(string? path);
    }
}
```

## Use Cases

1. **Backwards Compatibility**: Direct access via `GlobalState.SelectedNode` redirects to `SelectionContext` under the hood.
2. **Reactive UI Sync**: Inspector windows and project explorers register callbacks on `OnSelectionChanged` to update their fields immediately instead of using CPU-heavy polling loops.
3. **Undo/Redo Command Flow**: Changes to selection are wrapped inside `SelectNodeCommand` or `SelectResourceCommand` to record transaction history and allow reverting/re-applying navigation.

## Practical and Compilable Example

This example demonstrates how an editor panel registers to selection change events, reads selection values, and updates its visual representation reactively.

```csharp
using System;
using ImGuiNET;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Windows
{
    public class SelectionInspectorPanel
    {
        private readonly SelectionContext _context;
        private string _statusText = "No active selection";

        public SelectionInspectorPanel(SelectionContext context)
        {
            _context = context;
            // Subscribe reactively to selection changes
            _context.OnSelectionChanged += HandleSelectionChanged;
        }

        private void HandleSelectionChanged(object? newSelection)
        {
            if (newSelection is Guid entityId)
            {
                _statusText = $"Inspecting Entity ID: {entityId}";
            }
            else if (newSelection is string resourcePath)
            {
                _statusText = $"Inspecting Asset File: {resourcePath}";
            }
            else
            {
                _statusText = "Selection cleared";
            }
        }

        public void Draw()
        {
            ImGui.Begin("Selection Inspector");
            ImGui.TextColored(new System.Numerics.Vector4(0.1f, 0.8f, 0.8f, 1f), _statusText);
            
            if (_context.SelectedEntityId != Guid.Empty)
            {
                ImGui.BulletText($"Selected Entity ID: {_context.SelectedEntityId}");
            }
            ImGui.End();
        }
    }
}
```
