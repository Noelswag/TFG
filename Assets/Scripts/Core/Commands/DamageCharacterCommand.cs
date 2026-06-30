using Core.Model;

namespace Core.Commands
{
    public enum DamageType
    {
        Physical,
        Magic
    }

    public class DamageCharacterCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly int _amount;
        private readonly DamageType _damageType;
        private int _previousHP;

        /// <summary>
        /// Initializes a new instance of the DamageCharacterCommand class.
        /// Defaults to DamageType.Magic for spell/reaction backwards compatibility.
        /// </summary>
        public DamageCharacterCommand(ICharacter character, int amount, DamageType damageType = DamageType.Magic)
        {
            _character = character;
            _amount = amount;
            _damageType = damageType;
        }

        /// <summary>
        /// Stores the character's previous HP and applies modified damage based on terrain and status effects.
        /// </summary>
        public void Execute()
        {
            _previousHP = _character.CurrentHP;
            int finalAmount = _amount;

            ITileGrid grid = GridRegistry.Grid;
            ITile tile = (grid != null) ? grid.GetTile(_character.Position) : null;
            TerrainType currentTerrain = (tile != null) ? tile.CurrentTerrain : TerrainType.Ground;

            if (_damageType == DamageType.Physical)
            {
                // 1. Water complex effect: 25% physical damage reduction
                if (currentTerrain == TerrainType.Water)
                {
                    finalAmount = (int)(finalAmount * 0.75f);
                }

                // 2. Freeze reaction effect: 50% physical damage reduction
                if (_character.HasStatus("Freeze"))
                {
                    finalAmount /= 2;
                }
            }
            else if (_damageType == DamageType.Magic)
            {
                // 1. Tree complex effect: 50% magic damage amplification
                if (currentTerrain == TerrainType.Tree)
                {
                    finalAmount = (int)(finalAmount * 1.5f);
                }

                // 2. Mud complex effect: 50% magic damage reduction
                if (_character.HasStatus("MudTraversal"))
                {
                    finalAmount /= 2;
                }
            }

            _character.ApplyDamage(finalAmount);
        }

        /// <summary>
        /// Restores the exact amount of health lost, based on the stored previous HP.
        /// </summary>
        public void Undo()
        {
            int hpLost = _previousHP - _character.CurrentHP;
            if (hpLost > 0)
            {
                _character.ApplyHeal(hpLost);
            }
        }
    }
}
