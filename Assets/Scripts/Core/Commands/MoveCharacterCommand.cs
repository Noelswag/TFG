using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class MoveCharacterCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly GridPosition _targetPosition;
        private readonly List<GridPosition> _path;
        private readonly ITileGrid _grid;
        private readonly ICommandManager _subCommandManager;
        private GridPosition _previousPosition;

        /// <summary>
        /// Initializes a new instance of the MoveCharacterCommand class.
        /// </summary>
        public MoveCharacterCommand(ICharacter character, GridPosition targetPosition, List<GridPosition> path, ITileGrid grid)
        {
            _character = character;
            _targetPosition = targetPosition;
            _path = path;
            _grid = grid;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Executes the character movement along the path, applying traversal and stop terrain effects.
        /// </summary>
        public void Execute()
        {
            _previousPosition = _character.Position;

            // Revert any location-based evasion modifiers from the starting tile
            ITile startTile = _grid.GetTile(_previousPosition);
            if (startTile != null)
            {
                if (startTile.CurrentTerrain == TerrainType.Tree)
                {
                    _subCommandManager.ExecuteCommand(new ModifyEvasionCommand(_character, -15));
                }
                else if (startTile.CurrentTerrain == TerrainType.Ice)
                {
                    _subCommandManager.ExecuteCommand(new ModifyEvasionCommand(_character, 20));
                }
            }

            // Consume entire movement speed budget
            if (_character.CurrentMovementSpeed > 0f)
            {
                _subCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(_character, -_character.CurrentMovementSpeed));
            }

            foreach (GridPosition pos in _path)
            {
                _character.MoveTo(pos);

                ITile tile = _grid.GetTile(pos);
                if (tile != null && tile.Properties is ITraversalEffect traversalEffect)
                {
                    traversalEffect.ApplyOnTraversal(_character, tile, _subCommandManager);
                }
            }

            if (_character.Position != _targetPosition)
            {
                _character.MoveTo(_targetPosition);
            }

            ITile destinationTile = _grid.GetTile(_targetPosition);
            if (destinationTile != null && destinationTile.Properties is IStopEffect stopEffect)
            {
                stopEffect.ApplyOnStop(_character, destinationTile, _subCommandManager);
            }
        }

        /// <summary>
        /// Undoes the character movement, reverting all terrain side effects and stepping backward along the path.
        /// </summary>
        public void Undo()
        {
            while (_subCommandManager.CanUndo)
            {
                _subCommandManager.Undo();
            }

            if (_path != null && _path.Count > 0)
            {
                for (int i = _path.Count - 2; i >= 0; i--)
                {
                    _character.MoveTo(_path[i]);
                }
            }
            _character.MoveTo(_previousPosition);
        }
    }
}
