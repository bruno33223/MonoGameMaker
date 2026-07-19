using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public class SelectNodeCommand : IEditorCommand
    {
        private readonly SelectionContext _context;
        private readonly SceneSerializer.EntityInstance? _newNode;
        private readonly SceneSerializer.EntityInstance? _oldNode;

        public SelectNodeCommand(SelectionContext context, SceneSerializer.EntityInstance? newNode)
        {
            _context = context;
            _newNode = newNode;
            _oldNode = context.SelectedNode;
        }

        public void Execute() => _context.SetSelectedNodeInternal(_newNode);
        public void Undo() => _context.SetSelectedNodeInternal(_oldNode);
    }
}
