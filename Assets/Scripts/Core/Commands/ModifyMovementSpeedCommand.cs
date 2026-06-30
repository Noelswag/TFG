using Core.Model;

namespace Core.Commands
{
    public class ModifyMovementSpeedCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly float _offset;

        /// <summary>
        /// Initializes a new instance of the ModifyMovementSpeedCommand class.
        /// </summary>
        public ModifyMovementSpeedCommand(ICharacter character, float offset)
        {
            _character = character;
            _offset = offset;
        }

        /// <summary>
        /// Modifies the character's movement speed by the specified offset.
        /// </summary>
        public void Execute()
        {
            _character.ModifyMovementSpeed(_offset);
        }

        /// <summary>
        /// Reverts the speed modification by applying the negative offset.
        /// </summary>
        public void Undo()
        {
            _character.ModifyMovementSpeed(-_offset);
        }
    }
}
