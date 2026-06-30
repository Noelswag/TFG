using Core.Model;

namespace Core.Commands
{
    public class UpdateStatusDurationCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly string _statusName;
        private readonly int _newDuration;
        private readonly int _oldDuration;

        /// <summary>
        /// Initializes a new instance of the UpdateStatusDurationCommand class.
        /// </summary>
        public UpdateStatusDurationCommand(ICharacter character, string statusName, int newDuration)
        {
            _character = character;
            _statusName = statusName;
            _newDuration = newDuration;
            _oldDuration = character.GetStatusDuration(statusName);
        }

        /// <summary>
        /// Updates the remaining turn duration of the status effect.
        /// </summary>
        public void Execute()
        {
            _character.SetStatusDuration(_statusName, _newDuration);
        }

        /// <summary>
        /// Restores the status effect's previous remaining turn duration.
        /// </summary>
        public void Undo()
        {
            _character.SetStatusDuration(_statusName, _oldDuration);
        }
    }
}
