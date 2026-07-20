using System;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public class SelectionContext
    {
        private Guid _selectedEntityId = Guid.Empty;
        private string? _selectedResourcePath;

        public event Action<object?>? OnSelectionChanged;

        public Guid SelectedEntityId => _selectedEntityId;
        public string? SelectedResourcePath => _selectedResourcePath;

        public void SetSelectedEntityIdInternal(Guid id)
        {
            _selectedEntityId = id;
            OnSelectionChanged?.Invoke(id == Guid.Empty ? null : (object)id);
        }

        public void SetSelectedResourcePathInternal(string? path)
        {
            _selectedResourcePath = path;
            OnSelectionChanged?.Invoke(path);
        }
    }
}
