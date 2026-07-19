# CommandSystem

The CommandSystem implements the Command Design Pattern to encapsulate all state mutations in the Mono GameMaker IDE. This architectural model enables full transaction history tracking, clean execution isolation, and seamless Undo/Redo capability.

## API Signature

### IEditorCommand Interface
```csharp
namespace MonoGameMaker.IDE.Core
{
    public interface IEditorCommand
    {
        void Execute();
        void Undo();
    }
}
```

### CommandManager Class
```csharp
namespace MonoGameMaker.IDE.Core
{
    public class CommandManager
    {
        public void ExecuteCommand(IEditorCommand command);
        public void Undo();
        public void Redo();
        public int UndoCount { get; }
        public int RedoCount { get; }
        public void Clear();
    }
}
```

## Use Cases

1. **Undo/Redo**: Reverting and re-applying modifications like coordinates, names, entity insertions, deletions, and property changes.
2. **Flooding Prevention**: Combined with ImGui inputs using `IsItemDeactivatedAfterEdit()`, multiple continuous edits (like dragging position floats) are consolidated into a single command pushed to the stack only when the user finishes editing.
3. **Redo Invalidation**: Automatically clears the Redo history stack whenever the user performs a new, branch-splitting operation.

## Practical and Compilable Example

This example demonstrates how to implement a custom property editing command and execute it via the `CommandManager`.

```csharp
using System;
using ImGuiNET;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Commands
{
    // A command that toggles the visibility status of an entity node
    public class ToggleVisibilityCommand : IEditorCommand
    {
        private readonly SceneSerializer.EntityInstance _node;
        private readonly string _oldValue;
        private readonly string _newValue;

        public ToggleVisibilityCommand(SceneSerializer.EntityInstance node, bool visible)
        {
            _node = node;
            _newValue = visible.ToString();
            
            // Fetch old value if present, default to true
            if (!_node.CustomProperties.TryGetValue("Visible", out _oldValue!))
            {
                _oldValue = "True";
            }
        }

        public void Execute()
        {
            _node.CustomProperties["Visible"] = _newValue;
        }

        public void Undo()
        {
            _node.CustomProperties["Visible"] = _oldValue;
        }
    }

    public class InspectorVisibilityCheckbox
    {
        private readonly CommandManager _cmdManager;

        public InspectorVisibilityCheckbox(CommandManager cmdManager)
        {
            _cmdManager = cmdManager;
        }

        public void Draw(SceneSerializer.EntityInstance node)
        {
            node.CustomProperties.TryGetValue("Visible", out string? isVisibleStr);
            bool isVisible = isVisibleStr == null || bool.Parse(isVisibleStr);

            bool checkValue = isVisible;
            if (ImGui.Checkbox("Is Visible", ref checkValue))
            {
                // Instantiate and execute command
                var cmd = new ToggleVisibilityCommand(node, checkValue);
                _cmdManager.ExecuteCommand(cmd);
            }
        }
    }
}
```
