using System;

namespace Core.Commands
{
    public interface ICommandManager
    {
        /// <summary>
        /// Executes a command and adds it to history.
        /// </summary>
        void ExecuteCommand(ICommand command);

        /// <summary>
        /// Reverts the last command.
        /// </summary>
        void Undo();

        /// <summary>
        /// Redoes the last reverted command.
        /// </summary>
        void Redo();

        /// <summary>
        /// Clears all undo and redo history.
        /// </summary>
        void ClearHistory();

        bool CanUndo { get; }
        bool CanRedo { get; }
        event Action OnHistoryChanged;
    }
}
