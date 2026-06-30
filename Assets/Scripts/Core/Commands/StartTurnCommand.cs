using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class StartTurnCommand : ICommand
    {
        private readonly IEnumerable<ICharacter> _allCharacters;
        private readonly ITileGrid _grid;
        private readonly Team? _targetTeam;
        private readonly ICommandManager _subCommandManager;

        /// <summary>
        /// Initializes a new instance of the StartTurnCommand class.
        /// </summary>
        public StartTurnCommand(IEnumerable<ICharacter> allCharacters, ITileGrid grid, Team? targetTeam = null)
        {
            _allCharacters = allCharacters;
            _grid = grid;
            _targetTeam = targetTeam;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Processes start-of-turn effects for the specified team's characters (e.g. Grass healing).
        /// </summary>
        public void Execute()
        {
            foreach (var character in _allCharacters)
            {
                if (_targetTeam.HasValue && character.Team != _targetTeam.Value)
                {
                    continue;
                }
                // Reset movement speed budget to base speed (only if not Frozen)
                if (!character.HasStatus("Freeze"))
                {
                    float speedDeficit = character.BaseMovementSpeed - character.CurrentMovementSpeed;
                    if (speedDeficit != 0f)
                    {
                        _subCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(character, speedDeficit));
                    }
                }

                // Fire terrain complex effect: Clear FireImbued at start of turn
                if (character.HasStatus("FireImbued"))
                {
                    _subCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(character, "FireImbued"));
                }

                // Grass tile complex effect: Restore HP at beginning of turn
                ITile tile = _grid.GetTile(character.Position);
                if (tile != null && tile.CurrentTerrain == TerrainType.Grass)
                {
                    _subCommandManager.ExecuteCommand(new HealCharacterCommand(character, 10));
                }
            }
        }

        /// <summary>
        /// Reverts start-of-turn effects.
        /// </summary>
        public void Undo()
        {
            while (_subCommandManager.CanUndo)
            {
                _subCommandManager.Undo();
            }
        }
    }
}
