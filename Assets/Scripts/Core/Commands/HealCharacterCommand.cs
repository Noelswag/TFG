using Core.Model;

namespace Core.Commands
{
    public class HealCharacterCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly int _amount;
        private int _previousHP;

        /// <summary>
        /// Initializes a new instance of the HealCharacterCommand class.
        /// </summary>
        public HealCharacterCommand(ICharacter character, int amount)
        {
            _character = character;
            _amount = amount;
        }

        /// <summary>
        /// Stores the character's previous HP and applies the specified healing amount.
        /// </summary>
        public void Execute()
        {
            _previousHP = _character.CurrentHP;
            _character.ApplyHeal(_amount);
        }

        /// <summary>
        /// Reverts the healing by damaging the character by the exact amount of health restored.
        /// </summary>
        public void Undo()
        {
            int hpGained = _character.CurrentHP - _previousHP;
            if (hpGained > 0)
            {
                _character.ApplyDamage(hpGained);
            }
        }
    }
}
