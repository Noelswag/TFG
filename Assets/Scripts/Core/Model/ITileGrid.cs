using System;

namespace Core.Model
{
    public interface ITileGrid
    {
        int Width { get; }
        int Height { get; }

        /// <summary>
        /// Retrieves the tile at the specified position.
        /// </summary>
        ITile GetTile(GridPosition position);

        /// <summary>
        /// Checks if the specified position is within the grid boundaries.
        /// </summary>
        bool IsValidPosition(GridPosition position);

        event Action<ITile> OnTileModified;
    }
}
