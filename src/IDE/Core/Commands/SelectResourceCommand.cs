namespace MonoGameMaker.IDE.Core
{
    public class SelectResourceCommand : IEditorCommand
    {
        private readonly SelectionContext _context;
        private readonly string? _newPath;
        private readonly string? _oldPath;

        public SelectResourceCommand(SelectionContext context, string? newPath)
        {
            _context = context;
            _newPath = newPath;
            _oldPath = context.SelectedResourcePath;
        }

        public void Execute() => _context.SetSelectedResourcePathInternal(_newPath);
        public void Undo() => _context.SetSelectedResourcePathInternal(_oldPath);
    }
}
