using Core.Model;

namespace Core.Commands
{
    public class ModifyEvasionCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly int _offset;

        /// <summary>
        /// Initializes a new instance of the ModifyEvasionCommand class.
        /// </summary>
        public ModifyEvasionCommand(ICharacter character, int offset)
        {
            _character = character;
            _offset = offset;
        }

        /// <summary>
        /// Modifies the character's evasion modifier by the specified offset.
        /// </summary>
        public void Execute()
        {
            _character.ModifyEvasion(_offset);
        }

        /// <summary>
        /// Reverts the evasion modification by applying the negative offset.
        /// </summary>
        public void Undo()
        {
            _character.ModifyEvasion(-_offset);
        }
    }
}
