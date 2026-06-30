using Core.Model;

namespace Core.Commands
{
    public class RemoveStatusEffectCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly string _statusName;
        private bool _didRemove;
        private int _oldDuration;

        /// <summary>
        /// Initializes a new instance of the RemoveStatusEffectCommand class.
        /// </summary>
        public RemoveStatusEffectCommand(ICharacter character, string statusName)
        {
            _character = character;
            _statusName = statusName;
        }

        /// <summary>
        /// Removes the status effect from the character if they currently have it.
        /// </summary>
        public void Execute()
        {
            _didRemove = _character.HasStatus(_statusName);
            if (_didRemove)
            {
                _oldDuration = _character.GetStatusDuration(_statusName);
                _character.RemoveStatus(_statusName);
            }
        }

        /// <summary>
        /// Reverts the status effect removal if it was removed by this command.
        /// </summary>
        public void Undo()
        {
            if (_didRemove)
            {
                _character.AddStatus(_statusName);
                _character.SetStatusDuration(_statusName, _oldDuration);
            }
        }
    }
}
