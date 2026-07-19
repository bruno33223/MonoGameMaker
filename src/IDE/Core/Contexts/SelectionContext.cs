using System;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public class SelectionContext
    {
        private SceneSerializer.EntityInstance? _selectedNode;
        private string? _selectedResourcePath;

        public event Action<object?>? OnSelectionChanged;

        public SceneSerializer.EntityInstance? SelectedNode => _selectedNode;
        public string? SelectedResourcePath => _selectedResourcePath;

        public void SetSelectedNodeInternal(SceneSerializer.EntityInstance? node)
        {
            _selectedNode = node;
            OnSelectionChanged?.Invoke(node);
        }

        public void SetSelectedResourcePathInternal(string? path)
        {
            _selectedResourcePath = path;
            OnSelectionChanged?.Invoke(path);
        }
    }
}
