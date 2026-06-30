using System;
using System.Collections.Generic;

namespace Core.Commands
{
    public class CommandManager : ICommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event Action OnHistoryChanged;

        /// <summary>
        /// Executes a new command, pushes it to the undo stack, and clears the redo history.
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Undoes the last executed command and pushes it to the redo stack.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            ICommand command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Redoes the last undone command and pushes it back to the undo stack.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            ICommand command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Clears both undo and redo histories.
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
