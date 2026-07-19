namespace MonoGameMaker.IDE.Core
{
    public interface IEditorCommand
    {
        void Execute();
        void Undo();
    }
}
