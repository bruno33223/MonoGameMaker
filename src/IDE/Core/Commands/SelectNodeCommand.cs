using System;
using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public class SelectNodeCommand : IEditorCommand
    {
        private readonly SelectionContext _context;
        private readonly Guid _newId;
        private readonly Guid _oldId;

        public SelectNodeCommand(SelectionContext context, SceneSerializer.EntityInstance? newNode)
        {
            _context = context;
            _newId = newNode?.Id ?? Guid.Empty;
            _oldId = context.SelectedEntityId;
        }

        public void Execute() => _context.SetSelectedEntityIdInternal(_newId);
        public void Undo() => _context.SetSelectedEntityIdInternal(_oldId);
    }
}
