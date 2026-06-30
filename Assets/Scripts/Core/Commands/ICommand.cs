namespace Core.Commands
{
    public interface ICommand
    {
        /// <summary>
        /// Executes the command action.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverts the command action.
        /// </summary>
        void Undo();
    }
}
