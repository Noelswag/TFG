using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class EndTurnCommand : ICommand
    {
        private readonly IEnumerable<ICharacter> _allCharacters;
        private readonly ITileGrid _grid;
        private readonly Team? _targetTeam;
        private readonly ICommandManager _subCommandManager;

        /// <summary>
        /// Initializes a new instance of the EndTurnCommand class.
        /// </summary>
        public EndTurnCommand(IEnumerable<ICharacter> allCharacters, ITileGrid grid, Team? targetTeam = null)
        {
            _allCharacters = allCharacters;
            _grid = grid;
            _targetTeam = targetTeam;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Processes start/end of turn ticks for characters and transforms expiring terrain.
        /// </summary>
        public void Execute()
        {
            // 1. Process character-specific turn ticks
            foreach (var character in _allCharacters)
            {
                if (_targetTeam.HasValue && character.Team != _targetTeam.Value)
                {
                    continue;
                }
                // Tick status durations (and expire them if duration becomes 0)
                List<string> activeStatuses = new List<string>(character.Statuses);
                foreach (string status in activeStatuses)
                {
                    int currentDuration = character.GetStatusDuration(status);
                    int newDuration = currentDuration - 1;

                    if (newDuration <= 0)
                    {
                        // The status has expired! Remove it and revert its stat modifications
                        _subCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(character, status));

                        if (status == "Freeze")
                        {
                            float speedDeficit = character.BaseMovementSpeed - character.CurrentMovementSpeed;
                            if (speedDeficit > 0)
                            {
                                _subCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(character, speedDeficit));
                            }
                        }
                        else if (status == "Blur")
                        {
                            _subCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(character, -2));
                        }
                        else if (status == "ExtinctionSpeedBoost")
                        {
                            _subCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(character, -2));
                        }
                        else if (status == "BurstEvasionBoost")
                        {
                            _subCommandManager.ExecuteCommand(new ModifyEvasionCommand(character, -15));
                        }
                    }
                    else
                    {
                        // Just decrement the duration
                        _subCommandManager.ExecuteCommand(new UpdateStatusDurationCommand(character, status, newDuration));
                    }
                }
            }

            // 2. Extinguish all Fire terrain to Ground after 1 turn passes
            if (!_targetTeam.HasValue || _targetTeam.Value == Team.Enemy)
            {
                int width = _grid.Width;
                int height = _grid.Height;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        ITile tile = _grid.GetTile(new GridPosition(x, y));
                        if (tile != null && tile.CurrentTerrain == TerrainType.Fire)
                        {
                            _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Ground));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverts all HP, status changes, and terrain updates executed at the end of the turn.
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
