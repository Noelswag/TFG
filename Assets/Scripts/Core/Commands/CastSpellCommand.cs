using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class CastSpellCommand : ICommand
    {
        private readonly GridPosition _targetPosition;
        private readonly string _element;
        private readonly ITileGrid _grid;
        private readonly IEnumerable<ICharacter> _allCharacters;
        private readonly ICommandManager _subCommandManager;

        /// <summary>
        /// Initializes a new instance of the CastSpellCommand class.
        /// </summary>
        public CastSpellCommand(GridPosition targetPosition, string element, ITileGrid grid, IEnumerable<ICharacter> allCharacters)
        {
            _targetPosition = targetPosition;
            _element = element;
            _grid = grid;
            _allCharacters = allCharacters;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Executes the spell effect on characters and terrain in a 3x3 area around the target position.
        /// </summary>
        public void Execute()
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    GridPosition pos = new GridPosition(_targetPosition.X + dx, _targetPosition.Y + dy);
                    if (_grid.IsValidPosition(pos))
                    {
                        ApplySpellSingleTile(pos, _element);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the spell's status effects and terrain transformations to a single grid coordinate.
        /// </summary>
        private void ApplySpellSingleTile(GridPosition pos, string element)
        {
            // 1. If a character is at that position, apply the status effect
            foreach (var character in _allCharacters)
            {
                if (character.Position == pos)
                {
                    _subCommandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, element, _grid, _allCharacters));
                }
            }

            // 2. Perform terrain transformations
            ITile tile = _grid.GetTile(pos);
            if (tile != null)
            {
                TerrainType current = tile.CurrentTerrain;

                if (element == "Fire")
                {
                    if (current == TerrainType.Grass || current == TerrainType.Tree)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Fire));
                    }
                    else if (current == TerrainType.Ice)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Water));
                    }
                }
                else if (element == "Ice")
                {
                    if (current == TerrainType.Fire)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Ground));
                    }
                    else if (current == TerrainType.Ground)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Mud));
                    }
                    else if (current == TerrainType.Water)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Ice));
                    }
                }
                else if (element == "Earth")
                {
                    if (current == TerrainType.Rock)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Ground));
                    }
                }
                else if (element == "Wind")
                {
                    if (current == TerrainType.Mud)
                    {
                        _subCommandManager.ExecuteCommand(new ChangeTerrainCommand(tile, TerrainType.Ground));
                    }
                    else if (current == TerrainType.Fire)
                    {
                        // Fire terrain + wind = nearby tiles affected by fire (meaning we apply Fire to neighbors)
                        foreach (GridPosition neighbor in pos.GetNeighbors())
                        {
                            if (_grid.IsValidPosition(neighbor))
                            {
                                ApplySpellSingleTile(neighbor, "Fire");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverts all terrain changes and character status adjustments made in the 3x3 spell area.
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
