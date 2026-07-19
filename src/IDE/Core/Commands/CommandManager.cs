using System;
using System.Collections.Generic;

namespace MonoGameMaker.IDE.Core
{
    public class CommandManager
    {
        private readonly Stack<IEditorCommand> _undoStack = new Stack<IEditorCommand>();
        private readonly Stack<IEditorCommand> _redoStack = new Stack<IEditorCommand>();

        public void ExecuteCommand(IEditorCommand command)
        {
            if (command == null) return;

            try
            {
                command.Execute();
                _undoStack.Push(command);
                _redoStack.Clear(); // Clear Redo stack when a new command is executed
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error executing command: {ex.Message}");
            }
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            try
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error undoing command: {ex.Message}");
            }
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            try
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error redoing command: {ex.Message}");
            }
        }

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
