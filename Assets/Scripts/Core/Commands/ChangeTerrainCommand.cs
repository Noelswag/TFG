using Core.Model;

namespace Core.Commands
{
    public class ChangeTerrainCommand : ICommand
    {
        private readonly ITile _tile;
        private readonly TerrainType _newTerrain;
        private TerrainType _previousTerrain;

        /// <summary>
        /// Initializes a new instance of the ChangeTerrainCommand class.
        /// </summary>
        public ChangeTerrainCommand(ITile tile, TerrainType newTerrain)
        {
            _tile = tile;
            _newTerrain = newTerrain;
        }

        /// <summary>
        /// Stores the previous terrain and sets the tile's terrain to the new terrain type.
        /// </summary>
        public void Execute()
        {
            _previousTerrain = _tile.CurrentTerrain;
            _tile.SetTerrain(_newTerrain);
        }

        /// <summary>
        /// Reverts the tile's terrain back to the stored previous terrain type.
        /// </summary>
        public void Undo()
        {
            _tile.SetTerrain(_previousTerrain);
        }
    }
}
